// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Devices.Client
{
    internal abstract class InternalBufferManager
    {
        protected InternalBufferManager()
        {
        }

        public abstract byte[] TakeBuffer(int bufferSize);

        public abstract void ReturnBuffer(byte[] buffer);

        public abstract void Clear();

        public static InternalBufferManager Create(long maxBufferPoolSize, int maxBufferSize, bool isTransportBufferPool)
        {
            if (maxBufferPoolSize == 0)
            {
                return GCBufferManager.Value;
            }
            else
            {
                Fx.Assert(maxBufferPoolSize > 0 && maxBufferSize >= 0, "bad params, caller should verify");
                return isTransportBufferPool
                    ? new PreallocatedBufferManager(maxBufferPoolSize, maxBufferSize)
                    : (InternalBufferManager)new PooledBufferManager(maxBufferPoolSize, maxBufferSize);
            }
        }

        public static byte[] AllocateByteArray(int size)
        {
            // This will be inlined in retail bits but provides a
            // common entry point for debugging all buffer allocations
            // and can be instrumented if necessary.
            return new byte[size];
        }

        private class PreallocatedBufferManager : InternalBufferManager
        {
            private readonly int _maxBufferSize;
            private readonly int _medBufferSize;
            private readonly int _smallBufferSize;

            private byte[][] _buffersList;
            private readonly GCHandle[] _handles;
            private readonly ConcurrentStack<byte[]> _freeSmallBuffers;
            private readonly ConcurrentStack<byte[]> _freeMedianBuffers;
            private readonly ConcurrentStack<byte[]> _freeLargeBuffers;

            internal PreallocatedBufferManager(long maxMemoryToPool, int maxBufferSize)
            {
                // default values: maxMemoryToPool = 48MB, maxBufferSize = 64KB
                // This creates the following buffers:
                // max: 64KB = 256, med 16KB = 1024, small 4KB = 4096
                _maxBufferSize = maxBufferSize;
                _medBufferSize = maxBufferSize / 4;
                _smallBufferSize = maxBufferSize / 16;

                long eachPoolSize = maxMemoryToPool / 3;
                long numLargeBuffers = eachPoolSize / maxBufferSize;
                long numMedBuffers = eachPoolSize / _medBufferSize;
                long numSmallBuffers = eachPoolSize / _smallBufferSize;
                long numBuffers = numLargeBuffers + numMedBuffers + numSmallBuffers;

                _buffersList = new byte[numBuffers][];
                _handles = new GCHandle[numBuffers];
                _freeSmallBuffers = new ConcurrentStack<byte[]>();
                _freeMedianBuffers = new ConcurrentStack<byte[]>();
                _freeLargeBuffers = new ConcurrentStack<byte[]>();

                int lastLarge = 0;
                for (int i = 0; i < numLargeBuffers; i++, lastLarge++)
                {
                    _buffersList[i] = new byte[maxBufferSize];
                    _handles[i] = GCHandle.Alloc(_buffersList[i], GCHandleType.Pinned);
                    _freeLargeBuffers.Push(_buffersList[i]);
                }

                int lastMed = lastLarge;
                for (int i = lastLarge; i < numMedBuffers + lastLarge; i++, lastMed++)
                {
                    _buffersList[i] = new byte[_medBufferSize];
                    _handles[i] = GCHandle.Alloc(_buffersList[i], GCHandleType.Pinned);
                    _freeMedianBuffers.Push(_buffersList[i]);
                }

                for (int i = lastMed; i < numSmallBuffers + lastMed; i++)
                {
                    _buffersList[i] = new byte[_smallBufferSize];
                    _handles[i] = GCHandle.Alloc(_buffersList[i], GCHandleType.Pinned);
                    _freeSmallBuffers.Push(_buffersList[i]);
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                if (bufferSize > _maxBufferSize)
                {
                    return null;
                }

                byte[] returnedBuffer;
                if (bufferSize <= _smallBufferSize)
                {
                    _ = _freeSmallBuffers.TryPop(out returnedBuffer);
                    return returnedBuffer;
                }

                if (bufferSize <= _medBufferSize)
                {
                    _ = _freeMedianBuffers.TryPop(out returnedBuffer);
                    return returnedBuffer;
                }

                _ = _freeLargeBuffers.TryPop(out returnedBuffer);
                return returnedBuffer;
            }

            /// <summary>
            /// Returned buffer must have been acquired via a call to TakeBuffer
            /// </summary>
            /// <param name="buffer"></param>
            public override void ReturnBuffer(byte[] buffer)
            {
                if (buffer.Length <= _smallBufferSize)
                {
                    _freeSmallBuffers.Push(buffer);
                }
                else if (buffer.Length <= _medBufferSize)
                {
                    _freeMedianBuffers.Push(buffer);
                }
                else
                {
                    _freeLargeBuffers.Push(buffer);
                }
            }

            public override void Clear()
            {
                for (int i = 0; i < _buffersList.Length; i++)
                {
                    _handles[i].Free();
                    _buffersList[i] = null;
                }
                _buffersList = null;
                _freeSmallBuffers.Clear();
                _freeMedianBuffers.Clear();
                _freeLargeBuffers.Clear();
            }
        }

        private class PooledBufferManager : InternalBufferManager
        {
            private const int MinBufferSize = 128;
            private const int MaxMissesBeforeTuning = 8;
            private const int InitialBufferCount = 1;
            private readonly object _tuningLock;

            private readonly int[] _bufferSizes;
            private readonly BufferPool[] _bufferPools;
            private long _remainingMemory;
            private bool _areQuotasBeingTuned;
            private int _totalMisses;

            public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
            {
                _tuningLock = new object();
                _remainingMemory = maxMemoryToPool;
                var bufferPoolList = new List<BufferPool>();

                for (int bufferSize = MinBufferSize; ;)
                {
                    long bufferCountLong = _remainingMemory / bufferSize;

                    int bufferCount = bufferCountLong > int.MaxValue ? int.MaxValue : (int)bufferCountLong;

                    if (bufferCount > InitialBufferCount)
                    {
                        bufferCount = InitialBufferCount;
                    }

                    bufferPoolList.Add(BufferPool.CreatePool(bufferSize, bufferCount));

                    _remainingMemory -= (long)bufferCount * bufferSize;

                    if (bufferSize >= maxBufferSize)
                    {
                        break;
                    }

                    long newBufferSizeLong = (long)bufferSize * 2;

                    bufferSize = newBufferSizeLong > maxBufferSize
                        ? maxBufferSize
                        : (int)newBufferSizeLong;
                }

                _bufferPools = bufferPoolList.ToArray();
                _bufferSizes = new int[_bufferPools.Length];
                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    _bufferSizes[i] = _bufferPools[i].BufferSize;
                }
            }

            public override void Clear()
            {
                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];
                    bufferPool.Clear();
                }
            }

            private void ChangeQuota(ref BufferPool bufferPool, int delta)
            {
                BufferPool oldBufferPool = bufferPool;
                int newLimit = oldBufferPool.Limit + delta;
                var newBufferPool = BufferPool.CreatePool(oldBufferPool.BufferSize, newLimit);
                for (int i = 0; i < newLimit; i++)
                {
                    byte[] buffer = oldBufferPool.Take();
                    if (buffer == null)
                    {
                        break;
                    }
                    newBufferPool.Return(buffer);
                    newBufferPool.IncrementCount();
                }
                _remainingMemory -= oldBufferPool.BufferSize * delta;
                bufferPool = newBufferPool;
            }

            private void DecreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, -1);
            }

            private int FindMostExcessivePool()
            {
                long maxBytesInExcess = 0;
                int index = -1;

                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];

                    if (bufferPool.Peak < bufferPool.Limit)
                    {
                        long bytesInExcess = (bufferPool.Limit - bufferPool.Peak) * (long)bufferPool.BufferSize;

                        if (bytesInExcess > maxBytesInExcess)
                        {
                            index = i;
                            maxBytesInExcess = bytesInExcess;
                        }
                    }
                }

                return index;
            }

            private int FindMostStarvedPool()
            {
                long maxBytesMissed = 0;
                int index = -1;

                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];

                    if (bufferPool.Peak == bufferPool.Limit)
                    {
                        long bytesMissed = bufferPool.Misses * (long)bufferPool.BufferSize;

                        if (bytesMissed > maxBytesMissed)
                        {
                            index = i;
                            maxBytesMissed = bytesMissed;
                        }
                    }
                }

                return index;
            }

            private BufferPool FindPool(int desiredBufferSize)
            {
                for (int i = 0; i < _bufferSizes.Length; i++)
                {
                    if (desiredBufferSize <= _bufferSizes[i])
                    {
                        return _bufferPools[i];
                    }
                }

                return null;
            }

            private void IncreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, 1);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                Fx.Assert(buffer != null, "caller must verify");

                BufferPool bufferPool = FindPool(buffer.Length);
                if (bufferPool != null)
                {
                    if (buffer.Length != bufferPool.BufferSize)
                    {
                        throw Fx.Exception.Argument(nameof(buffer), CommonResources.BufferIsNotRightSizeForBufferManager);
                    }

                    if (bufferPool.Return(buffer))
                    {
                        bufferPool.IncrementCount();
                    }
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                Fx.Assert(bufferSize >= 0, "caller must ensure a non-negative argument");

                BufferPool bufferPool = FindPool(bufferSize);
                if (bufferPool != null)
                {
                    byte[] buffer = bufferPool.Take();
                    if (buffer != null)
                    {
                        bufferPool.DecrementCount();
                        return buffer;
                    }
                    if (bufferPool.Peak == bufferPool.Limit)
                    {
                        bufferPool.Misses++;
                        if (++_totalMisses >= MaxMissesBeforeTuning)
                        {
                            TuneQuotas();
                        }
                    }
                    return AllocateByteArray(bufferPool.BufferSize);
                }
                else
                {
                    return AllocateByteArray(bufferSize);
                }
            }

            private void TuneQuotas()
            {
                if (_areQuotasBeingTuned)
                {
                    return;
                }

                bool lockHeld = false;
                try
                {
                    Monitor.TryEnter(_tuningLock, ref lockHeld);

                    // Don't bother if another thread already has the lock
                    if (!lockHeld || _areQuotasBeingTuned)
                    {
                        return;
                    }

                    _areQuotasBeingTuned = true;
                }
                finally
                {
                    if (lockHeld)
                    {
                        Monitor.Exit(_tuningLock);
                    }
                }

                // find the "poorest" pool
                int starvedIndex = FindMostStarvedPool();
                if (starvedIndex >= 0)
                {
                    BufferPool starvedBufferPool = _bufferPools[starvedIndex];

                    if (_remainingMemory < starvedBufferPool.BufferSize)
                    {
                        // find the "richest" pool
                        int excessiveIndex = FindMostExcessivePool();
                        if (excessiveIndex >= 0)
                        {
                            // steal from the richest
                            DecreaseQuota(ref _bufferPools[excessiveIndex]);
                        }
                    }

                    if (_remainingMemory >= starvedBufferPool.BufferSize)
                    {
                        // give to the poorest
                        IncreaseQuota(ref _bufferPools[starvedIndex]);
                    }
                }

                // reset statistics
                for (int i = 0; i < _bufferPools.Length; i++)
                {
                    BufferPool bufferPool = _bufferPools[i];
                    bufferPool.Misses = 0;
                }

                _totalMisses = 0;
                _areQuotasBeingTuned = false;
            }

            private abstract class BufferPool
            {
                private int _count;

                public BufferPool(int bufferSize, int limit)
                {
                    BufferSize = bufferSize;
                    Limit = limit;
                }

                public int BufferSize { get; private set; }

                public int Limit { get; private set; }

                public int Misses { get; set; }

                public int Peak { get; private set; }

                public void Clear()
                {
                    OnClear();
                    _count = 0;
                }

                public void DecrementCount()
                {
                    int newValue = _count - 1;
                    if (newValue >= 0)
                    {
                        _count = newValue;
                    }
                }

                public void IncrementCount()
                {
                    int newValue = _count + 1;
                    if (newValue <= Limit)
                    {
                        _count = newValue;
                        if (newValue > Peak)
                        {
                            Peak = newValue;
                        }
                    }
                }

                internal abstract byte[] Take();

                internal abstract bool Return(byte[] buffer);

                internal abstract void OnClear();

                internal static BufferPool CreatePool(int bufferSize, int limit)
                {
                    // To avoid many buffer drops during training of large objects which
                    // get allocated on the LOH, we use the LargeBufferPool and for
                    // bufferSize < 85000, the SynchronizedPool. There is a 12 or 24(x64)
                    // byte overhead for an array so we use 85000-24=84976 as the limit
                    return bufferSize < 84976
                        ? new SynchronizedBufferPool(bufferSize, limit)
                        : (BufferPool)new LargeBufferPool(bufferSize, limit);
                }

                private class SynchronizedBufferPool : BufferPool
                {
                    private readonly SynchronizedPool<byte[]> _innerPool;

                    internal SynchronizedBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _innerPool = new SynchronizedPool<byte[]>(limit);
                    }

                    internal override void OnClear()
                    {
                        _innerPool.Clear();
                    }

                    internal override byte[] Take()
                    {
                        return _innerPool.Take();
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        return _innerPool.Return(buffer);
                    }
                }

                private class LargeBufferPool : BufferPool
                {
                    private readonly Stack<byte[]> _items;

                    internal LargeBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _items = new Stack<byte[]>(limit);
                    }

                    private object ThisLock => _items;

                    internal override void OnClear()
                    {
                        lock (ThisLock)
                        {
                            _items.Clear();
                        }
                    }

                    internal override byte[] Take()
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count > 0)
                            {
                                return _items.Pop();
                            }
                        }

                        return null;
                    }

                    internal override bool Return(byte[] buffer)
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count < Limit)
                            {
                                _items.Push(buffer);
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
        }

        private class GCBufferManager : InternalBufferManager
        {
            private GCBufferManager()
            {
            }

            public static GCBufferManager Value { get; } = new GCBufferManager();

            public override void Clear()
            {
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                return AllocateByteArray(bufferSize);
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                // do nothing, GC will reclaim this buffer
            }
        }
    }
}
