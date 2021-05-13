// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public abstract class DeviceAuthenticationWithTokenRefresh : AuthenticationWithTokenRefresh
    {
        internal const int DefaultTimeToLiveSeconds = 1 * 60;
        internal const int DefaultBufferPercentage = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithTokenRefresh"/> class using default
        /// TTL and TTL buffer time settings.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        public DeviceAuthenticationWithTokenRefresh(string deviceId)
            : this(deviceId, DefaultTimeToLiveSeconds, DefaultBufferPercentage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithTokenRefresh"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="suggestedTimeToLiveSeconds">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        public DeviceAuthenticationWithTokenRefresh(
            string deviceId,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage)
            : base(SetSasTokenSuggestedTimeToLiveSeconds(suggestedTimeToLiveSeconds), SetSasTokenRenewalBufferPercentage(timeBufferPercentage))
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            DeviceId = deviceId;
        }

        /// <summary>
        /// Gets the DeviceId.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionStringBuilder"/> instance based on a snapshot of the properties of
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionStringBuilder"/> instance.</returns>
        public override IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            iotHubConnectionStringBuilder = base.Populate(iotHubConnectionStringBuilder);
            iotHubConnectionStringBuilder.DeviceId = DeviceId;
            return iotHubConnectionStringBuilder;
        }

        private static int SetSasTokenSuggestedTimeToLiveSeconds(int suggestedTimeToLiveSeconds)
        {
            return suggestedTimeToLiveSeconds == 0
                ? DefaultTimeToLiveSeconds
                : suggestedTimeToLiveSeconds;
        }

        private static int SetSasTokenRenewalBufferPercentage(int timeBufferPercentage)
        {
            return timeBufferPercentage == 0
                ? DefaultBufferPercentage
                : timeBufferPercentage;
        }
    }
}
