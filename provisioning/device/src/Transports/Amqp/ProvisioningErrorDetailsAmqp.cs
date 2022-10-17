// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class ProvisioningErrorDetailsAmqp : ProvisioningErrorDetails
    {
        internal const string RetryAfterKey = "Retry-After";

        /// <summary>
        /// The time to wait before trying again if this error is transient
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }

        internal static TimeSpan? GetRetryAfterFromApplicationProperties(AmqpMessage amqpResponse, TimeSpan defaultInterval)
        {
            if (amqpResponse.ApplicationProperties != null
                && amqpResponse.ApplicationProperties.Map.TryGetValue(RetryAfterKey, out object retryAfter))
            {
                if (int.TryParse(retryAfter.ToString(), out int secondsToWait))
                {
                    var serviceRecommendedDelay = TimeSpan.FromSeconds(secondsToWait);

                    return serviceRecommendedDelay.TotalSeconds < defaultInterval.TotalSeconds
                        ? defaultInterval
                        : serviceRecommendedDelay;
                }
            }

            return null;
        }

        internal static TimeSpan? GetRetryAfterFromRejection(Rejected rejected, TimeSpan defaultInterval)
        {
            if (rejected.Error?.Info != null
                && rejected.Error.Info.TryGetValue(RetryAfterKey, out object retryAfter)
                && int.TryParse(retryAfter.ToString(), out int secondsToWait))
            {
                return secondsToWait < defaultInterval.Seconds
                    ? defaultInterval
                    : TimeSpan.FromSeconds(secondsToWait);
            }

            return null;
        }
    }
}
