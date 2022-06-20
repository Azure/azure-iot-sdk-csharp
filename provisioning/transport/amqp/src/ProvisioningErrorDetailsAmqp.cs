﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Is instantiated by json converter")]
    internal class ProvisioningErrorDetailsAmqp : ProvisioningErrorDetails
    {
        /// <summary>
        /// The time to wait before trying again if this error is transient
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }

        public const string RetryAfterKey = "Retry-After";

        public static TimeSpan? GetRetryAfterFromApplicationProperties(AmqpMessage amqpResponse, TimeSpan defaultInterval)
        {
            if (amqpResponse.ApplicationProperties != null && amqpResponse.ApplicationProperties.Map.TryGetValue(RetryAfterKey, out object retryAfter))
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

        public static TimeSpan? GetRetryAfterFromRejection(Rejected rejected, TimeSpan defaultInterval)
        {
            if (rejected.Error != null && rejected.Error.Info != null)
            {
                if (rejected.Error.Info.TryGetValue(RetryAfterKey, out object retryAfter))
                {
                    if (int.TryParse(retryAfter.ToString(), out int secondsToWait))
                    {
                        return secondsToWait < defaultInterval.Seconds
                            ? defaultInterval
                            : TimeSpan.FromSeconds(secondsToWait);
                    }
                }

            }

            return null;
        }
    }
}
