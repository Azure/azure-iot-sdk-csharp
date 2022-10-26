// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class IotHubServiceRetryPolicyBase : IIotHubServiceRetryPolicy
    {
        private readonly Random _rng = new();
        private readonly object _rngLock = new();

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries; use a negative value for infinite retries.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throw if the value of <paramref name="maxRetries"/> is negative.</exception>
        protected IotHubServiceRetryPolicyBase(uint maxRetries)
        {
            if (maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, string.Format(CultureInfo.CurrentCulture, "ArgumentCannotBeNegative", new object[]
                {
                    nameof(maxRetries)
                }));
            }


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

            return (lastException is IotHubServiceException hubEx
                    && hubEx.IsTransient)
                && (MaxRetries == 0
                || currentRetryCount < MaxRetries);
        }

        /// <summary>
        /// Gets jitter between 95% and 105% of the base time.
        /// </summary>
        protected TimeSpan UpdateWithJitter(double baseTimeMs)
        {
            // Don't calculate jitter if the value is very small
            if (baseTimeMs < 50)
            {
                return TimeSpan.FromMilliseconds(baseTimeMs);
            }

            double jitterMs;

            // Because Random is not threadsafe
            lock (_rngLock)
            {
                // A random double from 95% to 105% of the baseTimeMs
                jitterMs = _rng.Next(95, 106);
            }

            jitterMs *= baseTimeMs / 100.0;

            return TimeSpan.FromMilliseconds(jitterMs);
        }
    }
}
