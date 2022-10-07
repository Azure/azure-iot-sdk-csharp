// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a fixed retry delay with jitter.
    /// </summary>
    /// <remarks>
    /// Jitter can be under 1 second, plus or minus.
    /// </remarks>
    public class FixedDelayRetryPolicy : RetryPolicyBase
    {
        private readonly TimeSpan _fixedDelay;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="fixedDelay">The fixed delay to wait between retries.</param>
        public FixedDelayRetryPolicy(uint maxRetries, TimeSpan fixedDelay)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(maxRetries, nameof(maxRetries));
            Argument.AssertNotNegativeValue(fixedDelay.Ticks, nameof(fixedDelay));

            _fixedDelay = fixedDelay;
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

            TimeSpan jitter = GetJitter(_fixedDelay.TotalMilliseconds);

            double actualWaitMs = _fixedDelay.TotalMilliseconds + jitter.TotalMilliseconds;

            // Because jitter could be negative, protect the result with absolute value.
            retryInterval = TimeSpan.FromMilliseconds(Math.Abs(actualWaitMs));

            return true;
        }
    }
}
