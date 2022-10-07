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
                || MaxRetries > 0
                    && currentRetryCount < MaxRetries;
        }

        /// <summary>
        /// Gets jitter up to a second, plus or minus.
        /// </summary>
        protected TimeSpan GetJitter(double baseTimeMs)
        {
            double jitterMs;
            // Because Random is not threadsafe
            lock (_rngLock)
            {
                int plusOrMinus = _rng.Next(0, 2) * 2 - 1;

                // a random double from 0 to 999, positive or negative
                double maxJitter = Math.Min(baseTimeMs, 1000);
                jitterMs = plusOrMinus * _rng.NextDouble() * maxJitter;
            }

            return TimeSpan.FromMilliseconds(jitterMs);
        }
    }
}
