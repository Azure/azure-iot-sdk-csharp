// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Devices
{
    internal class IOThreadTimerSlim : IDisposable
    {
        private Timer _timer;
        private readonly Action<object> _callback;
        private readonly object _callbackState;
        private SemaphoreSlim _sem;

        public IOThreadTimerSlim(Action<object> callback, object callbackState)
        {
            _sem = new SemaphoreSlim(1);
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

            _sem.Wait();

            _timer.Change(timeFromNow, Timeout.InfiniteTimeSpan);

            _sem.Release();
        }

        public bool Cancel()
        {
            _sem.Wait();

            _timer?.Dispose();
            _timer = null;

            _sem.Release();

            return true;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _sem?.Dispose();
        }

        private void CreateTimer()
        {
            _sem.Wait();
            _timer = new Timer((obj) => _callback(obj), _callbackState, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _sem.Release();
        }
    }
}
