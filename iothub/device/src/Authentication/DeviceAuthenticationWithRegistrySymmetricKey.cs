// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses the symmetric key associated with the device in the device registry.
    /// </summary>
    public sealed class DeviceAuthenticationWithRegistrySymmetricKey : IAuthenticationMethod
    {
        private string _deviceId;
        private byte[] _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithRegistrySymmetricKey"/> class.
        /// </summary>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="key">Symmetric key associated with the device.</param>
        public DeviceAuthenticationWithRegistrySymmetricKey(string deviceId, string key)
        {
            SetDeviceId(deviceId);
            SetKeyFromBase64String(key);
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
        /// Gets or sets the key associated with the device.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Cannot change property types on public classes.")]
        public byte[] Key
        {
            get => _key;
            set => SetKey(value);
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
            if (iotHubConnectionCredentials == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionCredentials));
            }

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.SharedAccessKey = KeyAsBase64String;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
            iotHubConnectionCredentials.SharedAccessSignature = null;

            return iotHubConnectionCredentials;
        }

        private void SetKey(byte[] key)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        private void SetKeyFromBase64String(string key)
        {
            if (key.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!StringValidationHelper.IsBase64String(key))
            {
                throw new ArgumentException("Key must be base64 encoded");
            }

            _key = Convert.FromBase64String(key);
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Device Id cannot be null or white space.");
            }

            _deviceId = deviceId;
        }
    }
}
