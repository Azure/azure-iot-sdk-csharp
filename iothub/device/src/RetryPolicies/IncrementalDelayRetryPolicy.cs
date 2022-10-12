// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using an incrementally increasing retry delay with jitter.
    /// </summary>
    /// <remarks>
    /// Jitter can change the delay from 95% to 105% of the calculated time.
    /// </remarks>
    public class IncrementalDelayRetryPolicy : RetryPolicyBase
    {
        private readonly TimeSpan _delayIncrement;
        private readonly TimeSpan _maxDelay;
        private readonly bool _useJitter;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="delayIncrement"></param>
        /// <param name="maxDelay">The maximum amount of time to wait between retries.</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in clients retrying.</param>
        public IncrementalDelayRetryPolicy(uint maxRetries, TimeSpan delayIncrement, TimeSpan maxDelay, bool useJitter)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(delayIncrement.Ticks, nameof(delayIncrement));
            Argument.AssertNotNegativeValue(maxDelay.Ticks, nameof(maxDelay));

            _delayIncrement = delayIncrement;
            _maxDelay = maxDelay;
            _useJitter = useJitter;
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

            double waitDurationMs = Math.Min(
                currentRetryCount * _delayIncrement.TotalMilliseconds,
                _maxDelay.TotalMilliseconds);

            retryInterval = _useJitter
                ? UpdateWithJitter(waitDurationMs)
                : TimeSpan.FromMilliseconds(waitDurationMs);

            return true;
        }
    }
}
