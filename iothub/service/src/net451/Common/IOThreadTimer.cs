// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Microsoft.Azure.Devices.Common.Interop;

namespace Microsoft.Azure.Devices.Common
{
    // IOThreadTimer has several characteristics that are important for performance:
    // - Timers that expire benefit from being scheduled to run on IO threads using IOThreadScheduler.Schedule.
    // - The timer "waiter" thread is only allocated if there are set timers.
    // - The timer waiter thread itself is an IO thread, which allows it to go away if there is no need for it,
    //   and allows it to be reused for other purposes.
    // - After the timer count goes to zero, the timer waiter thread remains active for a bounded amount
    //   of time to wait for additional timers to be set.
    // - Timers are stored in an array-based priority queue to reduce the amount of time spent in updates, and
    //   to always provide O(1) access to the minimum timer (the first one that will expire).
    // - The standard textbook priority queue data structure is extended to allow efficient Delete in addition to
    //   DeleteMin for efficient handling of canceled timers.
    // - Timers that are typically set, then immediately canceled (such as a retry timer,
    //   or a flush timer), are tracked separately from more stable timers, to avoid having
    //   to update the waitable timer in the typical case when a timer is canceled.  Whether
    //   a timer instance follows this pattern is specified when the timer is constructed.
    // - Extending a timer by a configurable time delta (maxSkew) does not involve updating the
    //   waitable timer, or taking a lock.
    // - Timer instances are relatively cheap.  They share "heavy" resources like the waiter thread and
    //   waitable timer handle.
    // - Setting or canceling a timer does not typically involve any allocations.

    internal class IOThreadTimer : IDisposable
    {
        private const int MaxSkewInMillisecondsDefault = 100;
        private static long s_systemTimeResolutionTicks = -1;
        private readonly Action<object> _callback;
        private readonly object _callbackState;
        private long _dueTime;
        private int _index;
        private readonly long _maxSkew;
        private readonly TimerGroup _timerGroup;

        public IOThreadTimer(Action<object> callback, object callbackState, bool isTypicallyCanceledShortlyAfterBeingSet)
            : this(callback, callbackState, isTypicallyCanceledShortlyAfterBeingSet, MaxSkewInMillisecondsDefault)
        {
        }

        public IOThreadTimer(Action<object> callback, object callbackState, bool isTypicallyCanceledShortlyAfterBeingSet, int maxSkewInMilliseconds)
        {
            _callback = callback;
            _callbackState = callbackState;
            _maxSkew = Ticks.FromMilliseconds(maxSkewInMilliseconds);
            _timerGroup = isTypicallyCanceledShortlyAfterBeingSet
                ? TimerManager.Value.VolatileTimerGroup
                : TimerManager.Value.StableTimerGroup;
        }

        public static long SystemTimeResolutionTicks
        {
            get
            {
                if (s_systemTimeResolutionTicks == -1)
                {
                    s_systemTimeResolutionTicks = GetSystemTimeResolution();
                }
                return s_systemTimeResolutionTicks;
            }
        }

        [Fx.Tag.SecurityNote(Critical = "Calls critical method GetSystemTimeAdjustment", Safe = "method is a SafeNativeMethod")]
        private static long GetSystemTimeResolution()
        {
            if (UnsafeNativeMethods.GetSystemTimeAdjustment(out _, out uint increment, out _) != 0)
            {
                return increment;
            }

            // Assume the default, which is around 15 milliseconds.
            return 15 * TimeSpan.TicksPerMillisecond;
        }

        public bool Cancel()
        {
            return TimerManager.Value.Cancel(this);
        }

        public void SetIfValid(TimeSpan timeFromNow)
        {
            if (TimeSpan.Zero < timeFromNow && timeFromNow < TimeSpan.MaxValue)
            {
                Set(timeFromNow);
            }
        }

        public void Set(TimeSpan timeFromNow)
        {
            if (timeFromNow == TimeSpan.MaxValue)
            {
                throw Fx.Exception.Argument(nameof(timeFromNow), Resources.IOThreadTimerCannotAcceptMaxTimeSpan);
            }

            SetAt(Ticks.Add(Ticks.Now, Ticks.FromTimeSpan(timeFromNow)));
        }

        public void Set(int millisecondsFromNow)
        {
            SetAt(Ticks.Add(Ticks.Now, Ticks.FromMilliseconds(millisecondsFromNow)));
        }

        public void SetAt(long newDueTimeInTicks)
        {
            if (newDueTimeInTicks >= TimeSpan.MaxValue.Ticks || newDueTimeInTicks < 0)
            {
                throw Fx.Exception.ArgumentOutOfRange(
                    "newDueTime",
                    newDueTimeInTicks,
                    CommonResources.GetString(Resources.ArgumentOutOfRange, 0, TimeSpan.MaxValue.Ticks - 1));
            }

            TimerManager.Value.Set(this, newDueTimeInTicks);
        }

        public void Dispose()
        {
            _timerGroup.Dispose();
        }

        [Fx.Tag.SynchronizationObject(Blocking = false, Scope = Fx.Tag.Strings.AppDomain)]
        private class TimerManager : IDisposable
        {
            private const long MaxTimeToWaitForMoreTimers = 1000 * TimeSpan.TicksPerMillisecond;
            private bool _waitScheduled;
            private readonly Action<object> _onWaitCallback;

            [Fx.Tag.Queue(typeof(IOThreadTimer), Scope = Fx.Tag.Strings.AppDomain, StaleElementsRemovedImmediately = true)]
            static readonly TimerManager s_value = new TimerManager();

            [Fx.Tag.SynchronizationObject(Blocking = false)]
            private readonly WaitableTimer[] _waitableTimers;

            public TimerManager()
            {
                _onWaitCallback = new Action<object>(OnWaitCallback);
                StableTimerGroup = new TimerGroup();
                VolatileTimerGroup = new TimerGroup();
                _waitableTimers = new WaitableTimer[] { StableTimerGroup.WaitableTimer, VolatileTimerGroup.WaitableTimer };
            }

            private object ThisLock => this;
            public static TimerManager Value => s_value;
            public TimerGroup StableTimerGroup { get; }
            public TimerGroup VolatileTimerGroup { get; }

            public void Set(IOThreadTimer timer, long dueTime)
            {
                long timeDiff = dueTime - timer._dueTime;
                if (timeDiff < 0)
                {
                    timeDiff = -timeDiff;
                }

                if (timeDiff > timer._maxSkew)
                {
                    lock (ThisLock)
                    {
                        TimerGroup timerGroup = timer._timerGroup;
                        TimerQueue timerQueue = timerGroup.TimerQueue;

                        if (timer._index > 0)
                        {
                            if (timerQueue.UpdateTimer(timer, dueTime))
                            {
                                UpdateWaitableTimer(timerGroup);
                            }
                        }
                        else
                        {
                            if (timerQueue.InsertTimer(timer, dueTime))
                            {
                                UpdateWaitableTimer(timerGroup);

                                if (timerQueue.Count == 1)
                                {
                                    EnsureWaitScheduled();
                                }
                            }
                        }
                    }
                }
            }

            public bool Cancel(IOThreadTimer timer)
            {
                lock (ThisLock)
                {
                    if (timer._index > 0)
                    {
                        TimerGroup timerGroup = timer._timerGroup;
                        TimerQueue timerQueue = timerGroup.TimerQueue;

                        timerQueue.DeleteTimer(timer);

                        if (timerQueue.Count > 0)
                        {
                            UpdateWaitableTimer(timerGroup);
                        }
                        else
                        {
                            TimerGroup otherTimerGroup = GetOtherTimerGroup(timerGroup);
                            if (otherTimerGroup.TimerQueue.Count == 0)
                            {
                                long now = Ticks.Now;
                                long thisGroupRemainingTime = timerGroup.WaitableTimer.DueTime - now;
                                long otherGroupRemainingTime = otherTimerGroup.WaitableTimer.DueTime - now;
                                if (thisGroupRemainingTime > MaxTimeToWaitForMoreTimers
                                    && otherGroupRemainingTime > MaxTimeToWaitForMoreTimers)
                                {
                                    timerGroup.WaitableTimer.Set(Ticks.Add(now, MaxTimeToWaitForMoreTimers));
                                }
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            private void EnsureWaitScheduled()
            {
                if (!_waitScheduled)
                {
                    ScheduleWait();
                }
            }

            private TimerGroup GetOtherTimerGroup(TimerGroup timerGroup)
            {
                return ReferenceEquals(timerGroup, VolatileTimerGroup)
                    ? StableTimerGroup
                    : VolatileTimerGroup;
            }

            private void OnWaitCallback(object state)
            {
                WaitHandle.WaitAny(_waitableTimers);
                long now = Ticks.Now;
                lock (ThisLock)
                {
                    _waitScheduled = false;
                    ScheduleElapsedTimers(now);
                    ReactivateWaitableTimers();
                    ScheduleWaitIfAnyTimersLeft();
                }
            }

            private void ReactivateWaitableTimers()
            {
                ReactivateWaitableTimer(StableTimerGroup);
                ReactivateWaitableTimer(VolatileTimerGroup);
            }

            private static void ReactivateWaitableTimer(TimerGroup timerGroup)
            {
                TimerQueue timerQueue = timerGroup.TimerQueue;

                if (timerQueue.Count > 0)
                {
                    timerGroup.WaitableTimer.Set(timerQueue.MinTimer._dueTime);
                }
                else
                {
                    timerGroup.WaitableTimer.Set(long.MaxValue);
                }
            }

            private void ScheduleElapsedTimers(long now)
            {
                ScheduleElapsedTimers(StableTimerGroup, now);
                ScheduleElapsedTimers(VolatileTimerGroup, now);
            }

            private static void ScheduleElapsedTimers(TimerGroup timerGroup, long now)
            {
                TimerQueue timerQueue = timerGroup.TimerQueue;
                while (timerQueue.Count > 0)
                {
                    IOThreadTimer timer = timerQueue.MinTimer;
                    long timeDiff = timer._dueTime - now;
                    if (timeDiff <= timer._maxSkew)
                    {
                        timerQueue.DeleteMinTimer();
                        ActionItem.Schedule(timer._callback, timer._callbackState);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void ScheduleWait()
            {
                ActionItem.Schedule(_onWaitCallback, null);
                _waitScheduled = true;
            }

            private void ScheduleWaitIfAnyTimersLeft()
            {
                if (StableTimerGroup.TimerQueue.Count > 0
                    || VolatileTimerGroup.TimerQueue.Count > 0)
                {
                    ScheduleWait();
                }
            }

            private static void UpdateWaitableTimer(TimerGroup timerGroup)
            {
                WaitableTimer waitableTimer = timerGroup.WaitableTimer;
                IOThreadTimer minTimer = timerGroup.TimerQueue.MinTimer;
                long timeDiff = waitableTimer.DueTime - minTimer._dueTime;

                if (timeDiff < 0)
                {
                    timeDiff = -timeDiff;
                }

                if (timeDiff > minTimer._maxSkew)
                {
                    waitableTimer.Set(minTimer._dueTime);
                }
            }

            public void Dispose()
            {
                StableTimerGroup.Dispose();
                VolatileTimerGroup.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private class TimerGroup : IDisposable
        {
            public TimerGroup()
            {
                WaitableTimer = new WaitableTimer();
                WaitableTimer.Set(long.MaxValue);
                TimerQueue = new TimerQueue();
            }

            public TimerQueue TimerQueue { get; }
            public WaitableTimer WaitableTimer { get; }

            public void Dispose()
            {
                WaitableTimer.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private class TimerQueue
        {
            private IOThreadTimer[] _timers;

            public TimerQueue()
            {
                _timers = new IOThreadTimer[4];
            }

            public int Count { get; private set; }

            public IOThreadTimer MinTimer
            {
                get
                {
                    Fx.Assert(Count > 0, "Should have at least one timer in our queue.");
                    return _timers[1];
                }
            }
            public void DeleteMinTimer()
            {
                IOThreadTimer minTimer = MinTimer;
                DeleteMinTimerCore();
                minTimer._index = 0;
                minTimer._dueTime = 0;
            }

            public void DeleteTimer(IOThreadTimer timer)
            {
                int index = timer._index;

                Fx.Assert(index > 0, "");
                Fx.Assert(index <= Count, "");

                IOThreadTimer[] tempTimers = _timers;

                while (true)
                {
                    int parentIndex = index / 2;

                    if (parentIndex >= 1)
                    {
                        IOThreadTimer parentTimer = tempTimers[parentIndex];
                        tempTimers[index] = parentTimer;
                        parentTimer._index = index;
                    }
                    else
                    {
                        break;
                    }

                    index = parentIndex;
                }

                timer._index = 0;
                timer._dueTime = 0;
                tempTimers[1] = null;
                DeleteMinTimerCore();
            }

            public bool InsertTimer(IOThreadTimer timer, long dueTime)
            {
                Fx.Assert(timer._index == 0, "Timer should not have an index.");

                IOThreadTimer[] tempTimers = _timers;

                int index = Count + 1;

                if (index == tempTimers.Length)
                {
                    tempTimers = new IOThreadTimer[tempTimers.Length * 2];
                    Array.Copy(_timers, tempTimers, _timers.Length);
                    _timers = tempTimers;
                }

                Count = index;

                if (index > 1)
                {
                    while (true)
                    {
                        int parentIndex = index / 2;

                        if (parentIndex == 0)
                        {
                            break;
                        }

                        IOThreadTimer parent = tempTimers[parentIndex];

                        if (parent._dueTime > dueTime)
                        {
                            tempTimers[index] = parent;
                            parent._index = index;
                            index = parentIndex;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                tempTimers[index] = timer;
                timer._index = index;
                timer._dueTime = dueTime;
                return index == 1;
            }

            public bool UpdateTimer(IOThreadTimer timer, long newDueTime)
            {
                int index = timer._index;

                IOThreadTimer[] tempTimers = _timers;
                int tempCount = Count;

                Fx.Assert(index > 0, "");
                Fx.Assert(index <= tempCount, "");

                int parentIndex = index / 2;
                if (parentIndex == 0
                    || tempTimers[parentIndex]._dueTime <= newDueTime)
                {
                    int leftChildIndex = index * 2;
                    if (leftChildIndex > tempCount ||
                        tempTimers[leftChildIndex]._dueTime >= newDueTime)
                    {
                        int rightChildIndex = leftChildIndex + 1;
                        if (rightChildIndex > tempCount ||
                            tempTimers[rightChildIndex]._dueTime >= newDueTime)
                        {
                            timer._dueTime = newDueTime;
                            return index == 1;
                        }
                    }
                }

                DeleteTimer(timer);
                InsertTimer(timer, newDueTime);
                return true;
            }

            private void DeleteMinTimerCore()
            {
                int currentCount = Count;

                if (currentCount == 1)
                {
                    Count = 0;
                    _timers[1] = null;
                }
                else
                {
                    IOThreadTimer[] tempTimers = _timers;
                    IOThreadTimer lastTimer = tempTimers[currentCount];
                    Count = --currentCount;

                    int index = 1;
                    while (true)
                    {
                        int leftChildIndex = index * 2;

                        if (leftChildIndex > currentCount)
                        {
                            break;
                        }

                        int childIndex;
                        IOThreadTimer child;

                        if (leftChildIndex < currentCount)
                        {
                            IOThreadTimer leftChild = tempTimers[leftChildIndex];
                            int rightChildIndex = leftChildIndex + 1;
                            IOThreadTimer rightChild = tempTimers[rightChildIndex];

                            if (rightChild._dueTime < leftChild._dueTime)
                            {
                                child = rightChild;
                                childIndex = rightChildIndex;
                            }
                            else
                            {
                                child = leftChild;
                                childIndex = leftChildIndex;
                            }
                        }
                        else
                        {
                            childIndex = leftChildIndex;
                            child = tempTimers[childIndex];
                        }

                        if (lastTimer._dueTime > child._dueTime)
                        {
                            tempTimers[index] = child;
                            child._index = index;
                        }
                        else
                        {
                            break;
                        }

                        index = childIndex;

                        if (leftChildIndex >= currentCount)
                        {
                            break;
                        }
                    }

                    tempTimers[index] = lastTimer;
                    lastTimer._index = index;
                    tempTimers[currentCount + 1] = null;
                }
            }
        }

        [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.NonBlocking)]
        private class WaitableTimer : WaitHandle
        {
            [Fx.Tag.SecurityNote(Critical = "Call the critical CreateWaitableTimer method in TimerHelper",
                Safe = "Doesn't leak information or resources")]
            public WaitableTimer()
            {
#if NETSTANDARD1_3
                SetSafeWaitHandle(TimerHelper.CreateWaitableTimer());
#else
                SafeWaitHandle = TimerHelper.CreateWaitableTimer();
#endif
            }

            public long DueTime { get; private set; }

            [Fx.Tag.SecurityNote(Critical = "Call the critical Set method in TimerHelper",
                Safe = "Doesn't leak information or resources")]
            public void Set(long newDueTime)
            {
#if NETSTANDARD1_3
                dueTime = TimerHelper.Set(GetSafeWaitHandle(), newDueTime);
#else
                DueTime = TimerHelper.Set(SafeWaitHandle, newDueTime);
#endif
            }
            [Fx.Tag.SecurityNote(Critical = "Provides a set of unsafe methods used to work with the WaitableTimer")]
            [SecurityCritical]
            private static class TimerHelper
            {
                public static unsafe SafeWaitHandle CreateWaitableTimer()
                {
                    SafeWaitHandle handle = UnsafeNativeMethods.CreateWaitableTimer(IntPtr.Zero, false, null);
                    if (handle.IsInvalid)
                    {
                        Exception exception = new Win32Exception();
                        handle.SetHandleAsInvalid();
                        throw Fx.Exception.AsError(exception);
                    }

                    return handle;
                }

                public static unsafe long Set(SafeWaitHandle timer, long dueTime)
                {
                    if (!UnsafeNativeMethods.SetWaitableTimer(timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false))
                    {
                        throw Fx.Exception.AsError(new Win32Exception());
                    }

                    return dueTime;
                }
            }
        }
    }
}
