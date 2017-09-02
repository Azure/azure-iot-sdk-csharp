// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;

    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a randomized exponential back off scheme to determine the interval between retries.
    /// </summary>
    /// <param name="retryCount">The maximum number of retry attempts.</param>
    /// <param name="minBackoff">The minimum backoff time</param>
    /// <param name="maxBackoff">The maximum backoff time.</param>
    /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
    public class ExponentialBackoff : IRetryPolicy
    {
        private readonly TransientFaultHandling.ExponentialBackoff exponentialBackoffRetryStrategy;

        public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            this.exponentialBackoffRetryStrategy = new TransientFaultHandling.ExponentialBackoff(retryCount, minBackoff, maxBackoff, deltaBackoff);
        }

        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            return this.exponentialBackoffRetryStrategy.GetShouldRetry()(currentRetryCount, lastException, out retryInterval);
        }
    }
}
