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
    internal class CustomRetryPolicy : IotHubClientExponentialBackoffRetryPolicy
    {
        private readonly HashSet<Type> _exceptionsToBeRetried;
        private readonly ILogger _logger;

        internal CustomRetryPolicy(HashSet<Type> exceptionsToBeRetried, ILogger logger)
            : base(0, TimeSpan.FromMinutes(1), true)
        {
            _exceptionsToBeRetried = exceptionsToBeRetried ?? new();
            _logger = logger;
        }

        public override bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            bool shouldRetry;

            _logger.LogInformation($"Retry requested #{currentRetryCount} exception [{lastException.GetType()}: {lastException.Message}].");

            shouldRetry = base.ShouldRetry(currentRetryCount, lastException, out retryInterval);
            if (!shouldRetry
                && _exceptionsToBeRetried.Contains(lastException.GetType()))
            {
                // If the base class denied retry, but our extra logic confirmed retry, we'll use a fixed retry.
                retryInterval = TimeSpan.FromSeconds(15);
                shouldRetry = true;
            }

            if (shouldRetry)
            {
                _logger.LogInformation($"Retry #{currentRetryCount} approved with delay {retryInterval}.");
                return true;
            }

            _logger.LogError($"Retry requested #{currentRetryCount} but failed criteria for retry [with {lastException.GetType()}: {lastException.Message}], so giving up.");
            return false;
        }
    }
}