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
        private byte[] _key;

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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="suggestedTimeToLive"/> is a negative timespan, or if
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
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLive));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage));
            }

            SetKeyFromBase64String(key);
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
        /// Gets or sets the key associated with the device.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public byte[] Key
        {
            get => _key;
            set => _key = value ?? throw new InvalidOperationException("Shared access key cannot be null.");
        }

        /// <summary>
        /// Gets or sets the Base64 formatted key associated with the device.
        /// </summary>
        public string KeyAsBase64String
        {
            get => Convert.ToBase64String(Key);
            set => SetKeyFromBase64String(value);
        }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        public IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.SharedAccessKey = KeyAsBase64String;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
            iotHubConnectionCredentials.SharedAccessSignature = null;
            iotHubConnectionCredentials.SasTokenTimeToLive = _suggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = _timeBufferPercentage;

            return iotHubConnectionCredentials;
        }

        private void SetKeyFromBase64String(string key)
        {
            if (key.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Shared access key cannot be null or white space.");
            }

            if (!StringValidationHelper.IsBase64String(key))
            {
                throw new InvalidOperationException("Shared access key must be base64 encoded.");
            }

            _key = Convert.FromBase64String(key);
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
            _moduleId = moduleId;
        }
    }
}
