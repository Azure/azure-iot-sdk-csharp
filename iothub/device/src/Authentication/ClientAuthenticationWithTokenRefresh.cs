// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public abstract class ClientAuthenticationWithTokenRefresh : AuthenticationWithTokenRefresh
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or whitespace.
        /// </exception>
        public ClientAuthenticationWithTokenRefresh(
            string deviceId,
            string moduleId = default,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
            : base(suggestedTimeToLive,
                  timeBufferPercentage)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            DeviceId = deviceId;

            if (moduleId != null
                && moduleId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Module Id cannot be white space.");
            }
            ModuleId = moduleId;
        }

        /// <summary>
        /// Gets the device Id.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets the module Id.
        /// </summary>
        public string ModuleId { get; private set; }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        public override void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            base.Populate(ref iotHubConnectionCredentials);
            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
        }
    }
}
