// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token.
    /// </summary>
    public sealed class DeviceAuthenticationWithToken : IAuthenticationMethod
    {
        private string _deviceId;
        private string _token;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithToken"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="token">Security Token.</param>
        public DeviceAuthenticationWithToken(string deviceId, string token)
        {
            SetDeviceId(deviceId);
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
        public IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            if (iotHubConnectionCredentials == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionCredentials));
            }

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.SharedAccessSignature = Token;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;

            return iotHubConnectionCredentials;
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            _deviceId = deviceId;
        }

        private void SetToken(string token)
        {
            if (token.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!token.StartsWith(SharedAccessSignatureConstants.SharedAccessSignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Token must be of type SharedAccessSignature");
            }

            _token = token;
        }
    }
}
