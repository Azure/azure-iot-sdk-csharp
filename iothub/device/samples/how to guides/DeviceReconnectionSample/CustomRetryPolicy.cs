// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// An exponential backoff with jitter based retry policy that retries on additional exceptions
    /// not covered by the SDK, because in this application we wish to run as long as possible.
    /// </summary>
    internal class CustomRetryPolicy : IRetryPolicy
    {
        private const uint MaxRetryCount = uint.MaxValue;
        private const uint MaxExponent = 22; // Avoid integer overlow (max of 30) and clamp max wait to just over 1 hour (2^22 = 1.16 hours).

        private readonly Random _rng = new();
        private readonly object _randLock = new();
        private readonly HashSet<Type> _exceptionsToBeRetried;

        private readonly ILogger _logger;

        internal CustomRetryPolicy(HashSet<Type> exceptionsToBeRetried, ILogger logger)
        {
            _exceptionsToBeRetried = exceptionsToBeRetried ?? new();
            _logger = logger;
        }

        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;
            _logger.LogInformation($"Retry requested #{currentRetryCount} exception [{lastException.GetType()}: {lastException.Message}].");

            if (currentRetryCount > MaxRetryCount)
            {
                _logger.LogError($"Retry requested #{currentRetryCount} of {MaxRetryCount} so giving up.");
                return false;
            }

            if (lastException is IotHubClientException iotHubException
                && iotHubException.IsTransient
                || _exceptionsToBeRetried.Contains(lastException.GetType()))
            {
                double jitterMs;
                // Because Random is not threadsafe
                lock (_randLock)
                {
                    int plusOrMinus = _rng.Next(0, 2) * 2 - 1;

                    // a random double from 0 to 999, positive or negative
                    jitterMs = plusOrMinus * _rng.NextDouble() * 1000;
                }

                // Avoid integer overlow and clamp max wait.
                uint exponent = Math.Min(MaxExponent, currentRetryCount);

                // 2 to the power of the retry count gives us exponential back-off.
                // Because jitter could be negative, protect the result with absolute value.
                double exponentialIntervalMs = Math.Abs(Math.Pow(2.0, exponent) + jitterMs);

                retryInterval = TimeSpan.FromMilliseconds(exponentialIntervalMs);
                _logger.LogInformation($"Retry requested #{currentRetryCount} with calculated delay of {retryInterval}.");
                return true;
            }

            _logger.LogError($"Retry requested #{currentRetryCount} but failed criteria for retry [with {lastException.GetType()}: {lastException.Message}], so giving up.");
            return false;
        }
    }
}