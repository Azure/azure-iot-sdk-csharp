// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Devices.Client.Utilities;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses the symmetric key associated with the device/module in the device/module registry.
    /// </summary>
    public sealed class ClientAuthenticationWithRegistrySymmetricKey : IAuthenticationMethod
    {
        private const int DefaultSasRenewalBufferPercentage = 15;
        private static readonly TimeSpan s_defaultSasTimeToLive = TimeSpan.FromHours(1);

        private readonly TimeSpan _suggestedTimeToLive;
        private readonly int _timeBufferPercentage;

        private string _deviceId;
        private string _moduleId;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="key">Symmetric key associated with the device.</param>
        /// <param name="moduleId">Module identifier.</param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="suggestedTimeToLive"/> is a negative timespan, or if
        /// <paramref name="timeBufferPercentage"/> is outside the range 0-100.</exception>
        public ClientAuthenticationWithRegistrySymmetricKey(
            string key,
            string deviceId,
            string moduleId = default,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
        {
            if (suggestedTimeToLive < TimeSpan.Zero)
            {
                throw new ArgumentException("The TTL value cannot be negative.", nameof(suggestedTimeToLive));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentException("The time buffer percentage cannot be out of the range 0-100.", nameof(timeBufferPercentage));
            }

            Key = key;
            SetDeviceId(deviceId);
            SetModuleId(moduleId);

            _suggestedTimeToLive = suggestedTimeToLive == default
                ? s_defaultSasTimeToLive
                : suggestedTimeToLive;

            _timeBufferPercentage = timeBufferPercentage == default
                ? DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;
        }

        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        public string DeviceId
        {
            get => _deviceId;
            set => SetDeviceId(value);
        }

        /// <summary>
        /// Gets or sets the module identifier.
        /// </summary>
        public string ModuleId
        {
            get => _moduleId;
            set => SetModuleId(value);
        }

        /// <summary>
        /// Gets or sets the Base64-formatted shared access key associated with the device.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        public void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.SharedAccessKey = Key;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
            iotHubConnectionCredentials.SharedAccessSignature = null;
            iotHubConnectionCredentials.SasTokenTimeToLive = _suggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = _timeBufferPercentage;
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Device Id cannot be null or white space.");
            }

            _deviceId = deviceId;
        }

        private void SetModuleId(string moduleId)
        {
            // The module Id is optional so we only check whether it is whitespace or not here.
            if (moduleId != null
                && moduleId.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Module Id cannot be white space.");
            }

            _moduleId = moduleId;
        }
    }
}
