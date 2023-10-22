// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class ProvisioningServiceRetryPolicy : IRetryPolicy
    {
        private const string RetryAfterKey = "Retry-After";
        private const int MaxRetryCount = 10;

        private static readonly TimeSpan s_defaultRetryInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_maxBackoff = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_deltaBackoff = TimeSpan.FromMilliseconds(100);

        private static readonly IRetryPolicy s_exponentialBackoffRetryStrategy = new ExponentialBackoff(
            retryCount: MaxRetryCount,
            minBackoff: s_defaultRetryInterval,
            maxBackoff: s_maxBackoff,
            deltaBackoff: s_deltaBackoff);

        public bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;

            var provisioningException = lastException as ProvisioningServiceClientHttpException;

            if (provisioningException == null || currentRetryCount > MaxRetryCount)
            {
                return false;
            }
            else if ((int)provisioningException.StatusCode == 429) // HttpStatusCode.TooManyRequests is not available in net472
            {
                IDictionary<string, string> httpHeaders = provisioningException.Fields;
                bool retryAfterPresent = httpHeaders.TryGetValue(RetryAfterKey, out string retryAfter);

                retryInterval = retryAfterPresent
                    ? TimeSpan.FromSeconds(Convert.ToDouble(retryAfter))
                    : s_defaultRetryInterval;

                return true;
            }
            else if ((int)provisioningException.StatusCode > 500 && (int)provisioningException.StatusCode < 600)
            {
                return s_exponentialBackoffRetryStrategy.ShouldRetry(currentRetryCount, lastException, out retryInterval);
            }

            return false;
        }
    }
}
