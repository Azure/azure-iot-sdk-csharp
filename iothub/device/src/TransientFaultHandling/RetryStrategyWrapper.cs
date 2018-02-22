// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.TransientFaultHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the base implementation of the retry mechanism for unreliable actions and transient conditions.
    /// </summary>
    internal class RetryStrategyWrapper : RetryStrategy
    {
        internal readonly IRetryPolicy retryPolicy;

        public RetryStrategyWrapper(IRetryPolicy retryPolicy) : base("RetryStrategy", true)
        {
            this.retryPolicy = retryPolicy;
        }

        public override ShouldRetry GetShouldRetry()
        {
            return (int currentRetryCount, Exception lastException, out TimeSpan interval) => this.retryPolicy.ShouldRetry(currentRetryCount, lastException, out interval);
        }
    }
}
