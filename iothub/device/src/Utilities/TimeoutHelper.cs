// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    [DebuggerStepThrough]
    internal struct TimeoutHelper
    {
        private DateTime _deadline;
        private bool _isDeadlineSet;
        private readonly TimeSpan _originalTimeout;

        public static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(int.MaxValue);

        public TimeoutHelper(TimeSpan timeout) :
            this(timeout, false)
        {
        }

        public TimeoutHelper(TimeSpan timeout, bool startTimeout)
        {
            Debug.Assert(timeout >= TimeSpan.Zero, "timeout must be non-negative");

            _originalTimeout = timeout;
            _deadline = DateTime.MaxValue;
            _isDeadlineSet = timeout == TimeSpan.MaxValue;

            if (startTimeout && !_isDeadlineSet)
            {
                SetDeadline();
            }
        }

        public TimeSpan GetRemainingTime()
        {
            if (!_isDeadlineSet)
            {
                SetDeadline();
                return _originalTimeout;
            }

            if (_deadline == DateTime.MaxValue)
            {
                return TimeSpan.MaxValue;
            }

            TimeSpan remaining = _deadline - DateTime.UtcNow;
            return remaining <= TimeSpan.Zero
                ? TimeSpan.Zero
                : remaining;
        }

        private void SetDeadline()
        {
            Debug.Assert(!_isDeadlineSet, "TimeoutHelper deadline set twice.");
            _deadline = DateTime.UtcNow + _originalTimeout;

#if NOTIMEOUT
            deadline = DateTime.MaxValue;
#endif

            _isDeadlineSet = true;
        }
    }
}
