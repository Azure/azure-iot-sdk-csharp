// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using an incrementally increasing retry delay with jitter.
    /// </summary>
    /// <remarks>
    /// Jitter can be under 1 second, plus or minus.
    /// </remarks>
    public class IncrementingDelayRetryPolicy : RetryPolicyBase
    {
        private readonly TimeSpan _delayIncrement;
        private readonly TimeSpan _maxWait;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="delayIncrement"></param>
        /// <param name="maxWait">The maximum amount of time to wait between retries (will not exceed ~12.43 days).</param>
        public IncrementingDelayRetryPolicy(uint maxRetries, TimeSpan delayIncrement, TimeSpan maxWait)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(delayIncrement.Ticks, nameof(delayIncrement));
            Argument.AssertNotNegativeValue(maxWait.Ticks, nameof(maxWait));

            _delayIncrement = delayIncrement;
            _maxWait = maxWait;
        }

        /// <summary>
        /// Returns true if, based on the parameters, the operation should be retried.
        /// </summary>
        /// <param name="currentRetryCount">How many times the operation has been retried.</param>
        /// <param name="lastException">Operation exception.</param>
        /// <param name="retryInterval">Next retry should be performed after this time interval.</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        public override bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (!base.ShouldRetry(currentRetryCount, lastException, out retryInterval))
            {
                return false;
            }

            double waitDurationMs = currentRetryCount * _delayIncrement.TotalMilliseconds;

            double minDelayMs = Math.Min(waitDurationMs, _maxWait.TotalMilliseconds);
            TimeSpan jitter = GetJitter(minDelayMs);
            double actualWaitMs = minDelayMs + jitter.TotalMilliseconds;

            // Because jitter could be negative, protect the result with absolute value.
            retryInterval = TimeSpan.FromMilliseconds(Math.Abs(actualWaitMs));

            return true;
        }
    }
}
