// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    // A simple synchronized pool would simply lock a stack and push/pop on return/take.
    //
    // This implementation tries to reduce locking by exploiting the case where an item
    // is taken and returned by the same thread, which turns out to be common in our
    // scenarios.
    //
    // Initially, all the quota is allocated to a global (non-thread-specific) pool,
    // which takes locks.  As different threads take and return values, we record their IDs,
    // and if we detect that a thread is taking and returning "enough" on the same thread,
    // then we decide to "promote" the thread.  When a thread is promoted, we decrease the
    // quota of the global pool by one, and allocate a thread-specific entry for the thread
    // to store it's value.  Once this entry is allocated, the thread can take and return
    // it's value from that entry without taking any locks.  Not only does this avoid
    // locks, but it affinitizes pooled items to a particular thread.
    //
    // There are a couple of additional things worth noting:
    //
    // It is possible for a thread that we have reserved an entry for to exit.  This means
    // we will still have a entry allocated for it, but the pooled item stored there
    // will never be used.  After a while, we could end up with a number of these, and
    // as a result we would begin to exhaust the quota of the overall pool.  To mitigate this
    // case, we throw away the entire per-thread pool, and return all the quota back to
    // the global pool if we are unable to promote a thread (due to lack of space).  Then
    // the set of active threads will be re-promoted as they take and return items.
    //
    // You may notice that the code does not immediately promote a thread, and does not
    // immediately throw away the entire per-thread pool when it is unable to promote a
    // thread.  Instead, it uses counters (based on the number of calls to the pool)
    // and a threshold to figure out when to do these operations.  In the case where the
    // pool to misconfigured to have too few items for the workload, this avoids constant
    // promoting and rebuilding of the per thread entries.
    //
    // You may also notice that we do not use interlocked methods when adjusting statistics.
    // Since the statistics are a heuristic as to how often something is happening, they
    // do not need to be perfect.
    //
    [Fx.Tag.SynchronizationObject(Blocking = false)]
    internal class SynchronizedPool<T> where T : class
    {
        private const int MaxPendingEntries = 128;
        private const int MaxPromotionFailures = 64;
        private const int MaxReturnsBeforePromotion = 64;
        private const int MaxThreadItemsPerProcessor = 16;
        private Entry[] _entries;
        private readonly GlobalPool _globalPool;
        private readonly int _maxCount;
        private PendingEntry[] _pending;
        private int _promotionFailures;

        public SynchronizedPool(int maxCount)
        {
            int threadCount = maxCount;
            int maxThreadCount = MaxThreadItemsPerProcessor + SynchronizedPoolHelper.ProcessorCount;
            if (threadCount > maxThreadCount)
            {
                threadCount = maxThreadCount;
            }
            _maxCount = maxCount;
            _entries = new Entry[threadCount];
            _pending = new PendingEntry[4];
            _globalPool = new GlobalPool(maxCount);
        }

        private object ThisLock => this;

        public void Clear()
        {
            Entry[] entriesReference = _entries;

            for (int i = 0; i < entriesReference.Length; i++)
            {
                entriesReference[i]._value = null;
            }

            _globalPool.Clear();
        }

        private void HandlePromotionFailure(int thisThreadID)
        {
            int newPromotionFailures = _promotionFailures + 1;

            if (newPromotionFailures >= MaxPromotionFailures)
            {
                lock (ThisLock)
                {
                    _entries = new Entry[_entries.Length];

                    _globalPool.MaxCount = _maxCount;
                }

                PromoteThread(thisThreadID);
            }
            else
            {
                _promotionFailures = newPromotionFailures;
            }
        }

        private bool PromoteThread(int thisThreadID)
        {
            lock (ThisLock)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    int threadID = _entries[i]._threadId;

                    if (threadID == thisThreadID)
                    {
                        return true;
                    }
                    else if (threadID == 0)
                    {
                        _globalPool.DecrementMaxCount();
                        _entries[i]._threadId = thisThreadID;
                        return true;
                    }
                }
            }

            return false;
        }

        private void RecordReturnToGlobalPool(int thisThreadID)
        {
            PendingEntry[] localPending = _pending;

            for (int i = 0; i < localPending.Length; i++)
            {
                int threadID = localPending[i]._threadId;

                if (threadID == thisThreadID)
                {
                    int newReturnCount = localPending[i]._returnCount + 1;

                    if (newReturnCount >= MaxReturnsBeforePromotion)
                    {
                        localPending[i]._returnCount = 0;

                        if (!PromoteThread(thisThreadID))
                        {
                            HandlePromotionFailure(thisThreadID);
                        }
                    }
                    else
                    {
                        localPending[i]._returnCount = newReturnCount;
                    }
                    break;
                }
                else if (threadID == 0)
                {
                    break;
                }
            }
        }

        private void RecordTakeFromGlobalPool(int thisThreadID)
        {
            PendingEntry[] localPending = _pending;

            for (int i = 0; i < localPending.Length; i++)
            {
                int threadID = localPending[i]._threadId;

                if (threadID == thisThreadID)
                {
                    return;
                }
                else if (threadID == 0)
                {
                    lock (localPending)
                    {
                        if (localPending[i]._threadId == 0)
                        {
                            localPending[i]._threadId = thisThreadID;
                            return;
                        }
                    }
                }
            }

            if (localPending.Length >= MaxPendingEntries)
            {
                _pending = new PendingEntry[localPending.Length];
            }
            else
            {
                var newPending = new PendingEntry[localPending.Length * 2];
                Array.Copy(localPending, newPending, localPending.Length);
                _pending = newPending;
            }
        }

        public bool Return(T value)
        {
            int thisThreadID = Environment.CurrentManagedThreadId;

            if (thisThreadID == 0)
            {
                return false;
            }

            if (ReturnToPerThreadPool(thisThreadID, value))
            {
                return true;
            }

            return ReturnToGlobalPool(thisThreadID, value);
        }

        private bool ReturnToPerThreadPool(int thisThreadID, T value)
        {
            Entry[] entriesReference = _entries;

            for (int i = 0; i < entriesReference.Length; i++)
            {
                int threadID = entriesReference[i]._threadId;

                if (threadID == thisThreadID)
                {
                    if (entriesReference[i]._value == null)
                    {
                        entriesReference[i]._value = value;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (threadID == 0)
                {
                    break;
                }
            }

            return false;
        }

        private bool ReturnToGlobalPool(int thisThreadID, T value)
        {
            RecordReturnToGlobalPool(thisThreadID);

            return _globalPool.Return(value);
        }

        public T Take()
        {
            int thisThreadID = Environment.CurrentManagedThreadId;

            if (thisThreadID == 0)
            {
                return null;
            }

            T value = TakeFromPerThreadPool(thisThreadID);

            if (value != null)
            {
                return value;
            }

            return TakeFromGlobalPool(thisThreadID);
        }

        private T TakeFromPerThreadPool(int thisThreadID)
        {
            Entry[] entriesReference = _entries;

            for (int i = 0; i < entriesReference.Length; i++)
            {
                int threadID = entriesReference[i]._threadId;

                if (threadID == thisThreadID)
                {
                    T value = entriesReference[i]._value;

                    if (value != null)
                    {
                        entriesReference[i]._value = null;
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (threadID == 0)
                {
                    break;
                }
            }

            return null;
        }

        private T TakeFromGlobalPool(int thisThreadID)
        {
            RecordTakeFromGlobalPool(thisThreadID);

            return _globalPool.Take();
        }

        private struct Entry
        {
            public int _threadId;
            public T _value;
        }

        private struct PendingEntry
        {
            public int _returnCount;
            public int _threadId;
        }

        private static class SynchronizedPoolHelper
        {
            public static readonly int ProcessorCount = GetProcessorCount();

            [Fx.Tag.SecurityNote(Critical = "Asserts in order to get the processor count from the environment", Safe = "This data isn't actually protected so it's ok to leak")]
            private static int GetProcessorCount()
            {
                return Environment.ProcessorCount;
            }
        }

        [Fx.Tag.SynchronizationObject(Blocking = false)]
        private class GlobalPool
        {
            private readonly Stack<T> _items;

            private int _maxCount;

            public GlobalPool(int maxCount)
            {
                _items = new Stack<T>();
                _maxCount = maxCount;
            }

            public int MaxCount
            {
                get => _maxCount;
                set
                {
                    lock (ThisLock)
                    {
                        while (_items.Count > value)
                        {
                            _items.Pop();
                        }
                        _maxCount = value;
                    }
                }
            }

            private object ThisLock => this;

            public void DecrementMaxCount()
            {
                lock (ThisLock)
                {
                    if (_items.Count == _maxCount)
                    {
                        _items.Pop();
                    }
                    _maxCount--;
                }
            }

            public T Take()
            {
                if (_items.Count > 0)
                {
                    lock (ThisLock)
                    {
                        if (_items.Count > 0)
                        {
                            return _items.Pop();
                        }
                    }
                }
                return null;
            }

            public bool Return(T value)
            {
                if (_items.Count < MaxCount)
                {
                    lock (ThisLock)
                    {
                        if (_items.Count < MaxCount)
                        {
                            _items.Push(value);
                            return true;
                        }
                    }
                }
                return false;
            }

            public void Clear()
            {
                lock (ThisLock)
                {
                    _items.Clear();
                }
            }
        }
    }
}
