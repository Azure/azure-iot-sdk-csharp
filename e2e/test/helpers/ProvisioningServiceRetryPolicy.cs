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
        private const uint MaxRetryCount = 5;

        private static readonly TimeSpan s_defaultRetryInterval = TimeSpan.FromSeconds(5);

        private static readonly IRetryPolicy s_retryPolicy = new IncrementalDelayRetryPolicy(MaxRetryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));

        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;

            var provisioningException = lastException as DeviceProvisioningServiceException;

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
                return s_retryPolicy.ShouldRetry(currentRetryCount, lastException, out retryInterval);
            }

            return false;
        }
    }
}
