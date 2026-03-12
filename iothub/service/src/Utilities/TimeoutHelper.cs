// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.Devices.Common
{
    [DebuggerStepThrough]
    internal struct TimeoutHelper
    {
        private DateTime _deadline;
        private bool _deadlineSet;
        public static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(int.MaxValue);

        public TimeoutHelper(TimeSpan timeout) :
            this(timeout, false)
        {
        }

        public TimeoutHelper(TimeSpan timeout, bool startTimeout)
        {
            Debug.Assert(timeout >= TimeSpan.Zero, "timeout must be non-negative");

            OriginalTimeout = timeout;
            _deadline = DateTime.MaxValue;
            _deadlineSet = timeout == TimeSpan.MaxValue;

            if (startTimeout && !_deadlineSet)
            {
                SetDeadline();
            }
        }

        public TimeSpan OriginalTimeout { get; private set; }

        public static int ToMilliseconds(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                return Timeout.Infinite;
            }

            return timeout.Ticks / TimeSpan.TicksPerMillisecond > int.MaxValue
                ? int.MaxValue
                : (int)timeout.TotalMilliseconds;
        }

        public TimeSpan RemainingTime()
        {
            if (!_deadlineSet)
            {
                SetDeadline();
                return OriginalTimeout;
            }
            else if (_deadline == DateTime.MaxValue)
            {
                return TimeSpan.MaxValue;
            }
            else
            {
                TimeSpan remaining = _deadline - DateTime.UtcNow;
                return remaining > TimeSpan.Zero
                    ? remaining
                    : TimeSpan.Zero;
            }
        }

        private void SetDeadline()
        {
            Debug.Assert(!_deadlineSet, "TimeoutHelper deadline set twice.");
            _deadline = DateTime.UtcNow + OriginalTimeout;
            _deadlineSet = true;
        }
    }
}
