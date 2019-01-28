// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using Microsoft.Azure.Devices.Client.Extensions;

    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh. 
    /// </summary>
    public abstract class ModuleAuthenticationWithTokenRefresh : AuthenticationWithTokenRefresh
    {
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;
        private const int DefaultBufferPercentage = 15;

        /// <summary>
        /// Gets the ModuleId.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the DeviceId.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleAuthenticationWithTokenRefresh"/> class using default
        /// TTL and TTL buffer time settings.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        public ModuleAuthenticationWithTokenRefresh(string deviceId, string moduleId)
            : this(deviceId, moduleId, DefaultTimeToLiveSeconds, DefaultBufferPercentage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleAuthenticationWithTokenRefresh"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value. The implementations of this abstract
        /// may choose to ignore this value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as 
        /// a percentage of the time to live.</param>
        public ModuleAuthenticationWithTokenRefresh(
            string deviceId,
            string moduleId,
            int suggestedTimeToLiveSeconds,
            int timeBufferPercentage)
            : base(suggestedTimeToLiveSeconds, timeBufferPercentage)
        {
            if (moduleId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(moduleId));
            }

            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            ModuleId = moduleId;
            DeviceId = deviceId;
        }

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
            iotHubConnectionStringBuilder.ModuleId = ModuleId;
            return iotHubConnectionStringBuilder;
        }
    }
}
