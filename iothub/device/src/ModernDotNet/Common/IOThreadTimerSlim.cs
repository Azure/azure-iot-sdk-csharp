// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    internal class IOThreadTimerSlim
    {
        private Timer timer;
        private Action<object> callback;
        private object callbackState;

        private void CreateTimer()
        {
            this.timer = new Timer((obj) => callback(obj), callbackState, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Unknown if this is used by customers so we should leave as is to preserve the API.")]
        public IOThreadTimerSlim(Action<object> callback, object callbackState, bool isTypicallyCanceledShortlyAfterBeingSet)
        {
            this.callback = callback;
            this.callbackState = callbackState;
            CreateTimer();
        }

        public void Set(TimeSpan timeFromNow)
        {
            if (timer == null)
            {
                CreateTimer();
            }
            timer.Change(timeFromNow, TimeSpan.FromMilliseconds(-1));
        }

        public bool Cancel()
        {
            timer.Dispose();
            timer = null;
            return true;
        }
    }
}
