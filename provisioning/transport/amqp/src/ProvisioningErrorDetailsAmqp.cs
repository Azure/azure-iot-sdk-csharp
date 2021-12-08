// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Is instantiated by json convertor")]
    internal class ProvisioningErrorDetailsAmqp : ProvisioningErrorDetails
    {
        /// <summary>
        /// The time to wait before trying again if this error is transient
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }

        public const string RetryAfterKey = "Retry-After";

        public static TimeSpan? GetRetryAfterFromApplicationProperties(AmqpMessage amqpResponse, TimeSpan defaultInterval)
        {
            object retryAfter;
            if (amqpResponse.ApplicationProperties != null && amqpResponse.ApplicationProperties.Map.TryGetValue(RetryAfterKey, out retryAfter))
            {
                int secondsToWait;
                if (int.TryParse(retryAfter.ToString(), out secondsToWait))
                {
                    var serviceRecommendedDelay = TimeSpan.FromSeconds(secondsToWait);

                    if (serviceRecommendedDelay.TotalSeconds < defaultInterval.TotalSeconds)
                    {
                        return defaultInterval;
                    }
                    else
                    {
                        return serviceRecommendedDelay;
                    }
                }
            }

            return null;
        }

        public static TimeSpan? GetRetryAfterFromRejection(Rejected rejected, TimeSpan defaultInterval)
        {
            if (rejected.Error != null && rejected.Error.Info != null)
            {
                object retryAfter;
                if (rejected.Error.Info.TryGetValue(RetryAfterKey, out retryAfter))
                {
                    int secondsToWait = 0;
                    if (int.TryParse(retryAfter.ToString(), out secondsToWait))
                    {
                        if (secondsToWait < defaultInterval.Seconds)
                        {
                            return defaultInterval;
                        }
                        else
                        {
                            return TimeSpan.FromSeconds(secondsToWait);
                        }
                    }
                }
                
            }

            return null;
        }
    }
}
