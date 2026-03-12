// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Devices
{
    internal sealed class IOThreadTimerSlim : IDisposable
    {
        private Timer _timer;
        private readonly Action<object> _callback;
        private readonly object _callbackState;
        private readonly SemaphoreSlim _timerSemaphore;

        public IOThreadTimerSlim(Action<object> callback, object callbackState)
        {
            _timerSemaphore = new SemaphoreSlim(1);
            _callback = callback;
            _callbackState = callbackState;
            CreateTimer();
        }

        public void Set(TimeSpan timeFromNow)
        {
            if (_timer == null)
            {
                CreateTimer();
            }

            _timerSemaphore.Wait();

            _timer.Change(timeFromNow, Timeout.InfiniteTimeSpan);

            _timerSemaphore.Release();
        }

        public bool Cancel()
        {
            _timerSemaphore.Wait();

            _timer?.Dispose();
            _timer = null;

            _timerSemaphore.Release();

            return true;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timerSemaphore?.Dispose();
        }

        private void CreateTimer()
        {
            _timerSemaphore.Wait();
            _timer = new Timer((obj) => _callback(obj), _callbackState, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timerSemaphore.Release();
        }
    }
}
