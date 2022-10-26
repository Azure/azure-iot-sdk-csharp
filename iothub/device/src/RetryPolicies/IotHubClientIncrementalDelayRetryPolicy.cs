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
    public class IotHubClientIncrementalDelayRetryPolicy : IotHubClientRetryPolicyBase
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="delayIncrement">The amount to increment the delay on each additional count of retry.</param>
        /// <param name="maxDelay">The maximum amount of time to wait between retries.</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in clients retrying.</param>
        public IotHubClientIncrementalDelayRetryPolicy(uint maxRetries, TimeSpan delayIncrement, TimeSpan maxDelay, bool useJitter = true)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(delayIncrement.Ticks, nameof(delayIncrement));
            Argument.AssertNotNegativeValue(maxDelay.Ticks, nameof(maxDelay));

            DelayIncrement = delayIncrement;
            MaxDelay = maxDelay;
            UseJitter = useJitter;
        }

        /// <summary>
        /// The amount to increment the delay on each additional count of retry.
        /// </summary>
        internal protected TimeSpan DelayIncrement { get; }

        /// <summary>
        /// The maximum amount of time to wait between retries.
        /// </summary>
        internal protected TimeSpan MaxDelay { get; }

        /// <summary>
        /// Whether to add a small, random adjustment to the retry delay to avoid synchronicity in clients retrying.
        /// </summary>
        internal protected bool UseJitter { get; }

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
                currentRetryCount * DelayIncrement.TotalMilliseconds,
                MaxDelay.TotalMilliseconds);

            retryInterval = UseJitter
                ? UpdateWithJitter(waitDurationMs)
                : TimeSpan.FromMilliseconds(waitDurationMs);

            return true;
        }
    }
}
