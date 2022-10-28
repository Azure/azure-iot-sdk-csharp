// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class ProvisioningServiceRetryPolicy : IIotHubClientRetryPolicy
    {
        private const string RetryAfterKey = "Retry-After";
        private const uint MaxRetryCount = 20;
        private static readonly TimeSpan s_retryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_maxDelay = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan s_defaultRetryInterval = TimeSpan.FromSeconds(3);

        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;

            if (currentRetryCount > MaxRetryCount
                || lastException is not ProvisioningServiceException)
            {
                return false;
            }

            if (lastException is ProvisioningServiceException provisioningException)
            {
                if (!provisioningException.IsTransient)
                {
                    return false;
                }

                if ((int)provisioningException.StatusCode == 429) // HttpStatusCode.TooManyRequests is not available in net472
                {
                    IDictionary<string, string> httpHeaders = provisioningException.Fields;
                    bool retryAfterPresent = httpHeaders.TryGetValue(RetryAfterKey, out string retryAfter);

                    retryInterval = retryAfterPresent
                        ? TimeSpan.FromSeconds(Convert.ToDouble(retryAfter))
                        : s_defaultRetryInterval;

                    return true;
                }
            }

            double waitDurationMs = Math.Min(
                currentRetryCount * s_retryDelay.TotalMilliseconds,
                s_maxDelay.TotalMilliseconds);

            retryInterval = TimeSpan.FromMilliseconds(waitDurationMs);

            return true;
        }
    }
}
