// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a randomized exponential back off scheme to determine the interval between retries.
    /// </summary>
    public class ExponentialBackoff : IRetryPolicy
    {
        private readonly ExponentialBackoffRetryStrategy _exponentialBackoffRetryStrategy;

        /// <summary>
        /// Creates an instance of ExponentialBackoff.
        /// </summary>
        /// <param name="retryCount">The maximum number of retry attempts.</param>
        /// <param name="minBackoff">The minimum back-off time</param>
        /// <param name="maxBackoff">The maximum back-off time.</param>
        /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>

        public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            _exponentialBackoffRetryStrategy = new ExponentialBackoffRetryStrategy(retryCount, minBackoff, maxBackoff, deltaBackoff);
        }

        /// <summary>
        /// Returns true if, based on the parameters, the operation should be retried.
        /// </summary>
        /// <param name="currentRetryCount">How many times the operation has been retried.</param>
        /// <param name="lastException">Operation exception.</param>
        /// <param name="retryInterval">Next retry should be performed after this time interval.</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            return _exponentialBackoffRetryStrategy.GetShouldRetry()(currentRetryCount, lastException, out retryInterval);
        }
    }
}
