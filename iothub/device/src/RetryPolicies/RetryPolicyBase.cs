// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A base retry policy.
    /// </summary>
    public abstract class RetryPolicyBase : IRetryPolicy
    {
        private readonly Random _rng = new();
        private readonly object _rngLock = new();

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries; use a negative value for infinite retries.</param>
        protected RetryPolicyBase(uint maxRetries)
        {
            Argument.AssertNotNegativeValue(maxRetries, nameof(maxRetries));
            MaxRetries = maxRetries;
        }

        /// <summary>
        /// The maximum number of retries
        /// </summary>
        protected uint MaxRetries { get; private set; }

        /// <inheritdoc/>
        public virtual bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay)
        {
            retryDelay = TimeSpan.Zero;

            return lastException is IotHubClientException hubEx
                    && hubEx.IsTransient
                && MaxRetries == 0
                || currentRetryCount < MaxRetries;
        }

        /// <summary>
        /// Gets jitter between 95% and 105% of the base time.
        /// </summary>
        protected TimeSpan UpdateWithJitter(double baseTimeMs)
        {
            // Don't calculate jitter if the value is very small
            if (baseTimeMs < 10)
            {
                return TimeSpan.FromMilliseconds(baseTimeMs);
            }

            double jitterMs;

            // Because Random is not threadsafe
            lock (_rngLock)
            {
                // A random double from 95% to 105% of the baseTimeMs
                jitterMs = _rng.Next(95, 106) * baseTimeMs / 100.0;
            }

            return TimeSpan.FromMilliseconds(jitterMs);
        }
    }
}
