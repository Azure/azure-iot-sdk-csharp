// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token.
    /// </summary>
    public sealed class ClientAuthenticationWithToken : IAuthenticationMethod
    {
        private string _deviceId;
        private string _moduleId;
        private string _token;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="token">Security Token.</param>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        public ClientAuthenticationWithToken(string token, string deviceId, string moduleId = default)
        {
            SetDeviceId(deviceId);
            SetModuleId(moduleId);
            SetToken(token);
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
        /// Gets or sets the security token associated with the device.
        /// </summary>
        public string Token
        {
            get => _token;
            set => SetToken(value);
        }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="iotHubConnectionCredentials"/> is null.</exception>
        public IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.SharedAccessSignature = Token;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;

            return iotHubConnectionCredentials;
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

        private void SetToken(string token)
        {
            if (token.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Security token cannot be null or white space.");
            }

            if (!token.StartsWith(SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Security token must be of type SharedAccessSignature.");
            }

            _token = token;
        }
    }
}
