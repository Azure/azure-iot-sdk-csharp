// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a exponential back-off scheme with jitter
    /// to determine the interval between retries.
    /// </summary>
    /// <remarks>
    /// Jitter can be under 1 second, plus or minus.
    /// </remarks>
    public class ExponentialBackoffRetryPolicy : RetryPolicyBase
    {
        private const uint MaxExponent = 30; // Avoid integer overlow (max of 30) and clamp max wait to just over 1 hour (2^30 = ~12.43 days).
        private readonly TimeSpan _maxWait;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="maxWait">The maximum amount of time to wait between retries (will not exceed ~12.43 days).</param>
        public ExponentialBackoffRetryPolicy(uint maxRetries, TimeSpan maxWait)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(maxWait.Ticks, nameof(maxWait));

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

            // Avoid integer overlow and clamp max wait.
            uint exponent = Math.Min(MaxExponent, currentRetryCount);

            // 2 to the power of the retry count gives us exponential back-off.
            double exponentialIntervalMs = Math.Pow(2.0, exponent);

            TimeSpan jitter = GetJitter(exponentialIntervalMs);

            double actualWaitMs = Math.Min(exponentialIntervalMs, _maxWait.TotalMilliseconds) + jitter.TotalMilliseconds;

            // Because jitter could be negative, protect the result with absolute value.
            retryInterval = TimeSpan.FromMilliseconds(Math.Abs(actualWaitMs));

            return true;
        }
    }
}
