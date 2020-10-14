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

        public IOThreadTimerSlim(Action<object> callback, object callbackState)
        {
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
            _timer.Change(timeFromNow, TimeSpan.FromMilliseconds(-1));
        }

        public bool Cancel()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            return true;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void CreateTimer()
        {
            _timer = new Timer((obj) => _callback(obj), _callbackState, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }
    }
}
