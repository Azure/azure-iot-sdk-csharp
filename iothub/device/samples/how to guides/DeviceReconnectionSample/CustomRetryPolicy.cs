// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// An exponential backoff with jitter based retry policy that retries on additional exceptions
    /// not covered by the SDK, because in this application we wish to run as long as possible.
    /// </summary>
    internal class CustomRetryPolicy : IRetryPolicy
    {
        private const int MaxRetryCount = int.MaxValue;
        private const int MinExponent = 8; // Avoid retry storm and wait at least 256ms (2^8 ms).
        private const int MaxExponent = 20; // Avoid integer overlow (max of 20) and clamp max wait to 17.5 minutes (2^20 ms).
        private int _userExponent;

        private readonly Random _rng = new();
        private readonly object _randLock = new();
        private readonly HashSet<Type> _exceptionsToBeRetried;

        private readonly ILogger _logger;

        internal CustomRetryPolicy(HashSet<Type> exceptionsToBeRetried, ILogger logger, int exponent)
        {
            _exceptionsToBeRetried = exceptionsToBeRetried ?? new();
            _logger = logger;
            _userExponent = exponent<=MaxExponent?exponent:MaxExponent; // Avoid integer overlow and clamp max wait.
        }

        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;

            if (currentRetryCount > MaxRetryCount)
            {
                _logger.LogError($"Retry requested #{currentRetryCount} of {MaxRetryCount} so giving up.");
                return false;
            }

            if (lastException is IotHubException iotHubException
                && iotHubException.IsTransient
                || ExceptionHelper.IsNetworkExceptionChain(lastException)
                || _exceptionsToBeRetried.Contains(lastException.GetType()))
            {
                double jitterMs;
                // Because Random is not threadsafe
                lock (_randLock)
                {
                    int sign = _rng.Next(0, 2) * 2 - 1;

                    // a random double from 0 to 999, positive or negative
                    jitterMs =  sign * _rng.NextDouble() * 1000;
                }

                // Avoid integer overlow and clamp max wait.
                int exponent = Math.Min(_userExponent, currentRetryCount+MinExponent);

                // 2 to the power of the retry count gives us exponential back-off.
                // Because jitter could be negative, protect the result with absolute value.
                double exponentialIntervalMs = Math.Abs(Math.Pow(2.0, exponent) + jitterMs);

                retryInterval = TimeSpan.FromMilliseconds(exponentialIntervalMs);
                _logger.LogInformation($"Retry #{currentRetryCount} in {retryInterval}. Reason: [{lastException.GetType()}: {lastException.Message}]");
                return true;
            }

            _logger.LogError($"Retry requested #{currentRetryCount} but failed criteria for retry [with {lastException.GetType()}: {lastException.Message}], so giving up.");
            return false;
        }
    }
}