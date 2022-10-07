// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a exponential back-off scheme, with option jitter,
    /// to determine the interval between retries.
    /// </summary>
    /// <remarks>
    /// Jitter can change the delay from 95% to 105% of the calculated time.
    /// </remarks>
    public class ExponentialBackoffRetryPolicy : RetryPolicyBase
    {
        private const uint MaxExponent = 30; // Avoid integer overlow (max of 30) and clamp max wait to just over 1 hour (2^30 = ~12.43 days).
        private static readonly TimeSpan s_minDelay = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan _maxDelay;
        private readonly bool _useJitter;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="maxWait">The maximum amount of time to wait between retries (will not exceed ~12.43 days).</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in clients retrying.</param>
        public ExponentialBackoffRetryPolicy(uint maxRetries, TimeSpan maxWait, bool useJitter = true)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(maxWait.Ticks, nameof(maxWait));

            _maxDelay = maxWait;
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

            // Avoid integer overlow and clamp max wait.
            uint exponent = Math.Min(MaxExponent, currentRetryCount);

            // 2 to the power of the retry count gives us exponential back-off.
            double exponentialIntervalMs = Math.Pow(2.0, exponent) + s_minDelay.TotalMilliseconds;

            double clampedWaitMs = Math.Min(exponentialIntervalMs, _maxDelay.TotalMilliseconds);

            retryInterval = _useJitter
                ? UpdateWithJitter(clampedWaitMs)
                : TimeSpan.FromMilliseconds(clampedWaitMs);

            return true;
        }
    }
}
