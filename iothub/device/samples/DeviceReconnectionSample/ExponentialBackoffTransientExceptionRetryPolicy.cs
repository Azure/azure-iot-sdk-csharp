// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Exceptions;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// An exponential backoff based retry policy that retries on encountering transient exceptions.
    /// </summary>
    internal class ExponentialBackoffTransientExceptionRetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan s_minBackoff = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan s_maxBackoff = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_deltaBackoff = TimeSpan.FromMilliseconds(100);

        private readonly Random _random = new Random();
        private readonly int _maxRetryCount;
        private readonly IDictionary<IotHubStatusCode, string> _exceptionsToBeIgnored;

        internal ExponentialBackoffTransientExceptionRetryPolicy(int maxRetryCount = default, IDictionary<IotHubStatusCode, string> exceptionsToBeIgnored = default)
        {
            _maxRetryCount = maxRetryCount == 0 ? int.MaxValue : maxRetryCount;
            _exceptionsToBeIgnored = exceptionsToBeIgnored;
        }

        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (currentRetryCount < _maxRetryCount)
            {
                if ((lastException is IotHubClientException iotHubException
                        && (iotHubException.IsTransient || _exceptionsToBeIgnored != null && _exceptionsToBeIgnored.ContainsKey(iotHubException.StatusCode)))
                    || ExceptionHelper.IsNetworkExceptionChain(lastException))
                {
                    double exponentialInterval =
                        (Math.Pow(2.0, currentRetryCount) - 1.0)
                        * _random.Next(
                            (int)s_deltaBackoff.TotalMilliseconds * 8 / 10,
                            (int)s_deltaBackoff.TotalMilliseconds * 12 / 10)
                        + s_minBackoff.TotalMilliseconds;

                    double maxInterval = s_maxBackoff.TotalMilliseconds;
                    double num2 = Math.Min(exponentialInterval, maxInterval);
                    retryInterval = TimeSpan.FromMilliseconds(num2);
                    return true;
                }
            }

            retryInterval = TimeSpan.Zero;
            return false;
        }
    }
}
