// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a device's shared access signature to authenticate with service.
    /// </summary>
    public class ServiceAuthenticationWithDeviceSharedAccessPolicyToken : IAuthenticationMethod
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAuthenticationWithDeviceSharedAccessPolicyToken"/> class.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="sharedAccessSignature">The shared access signature.</param>
        public ServiceAuthenticationWithDeviceSharedAccessPolicyToken(string deviceId, string sharedAccessSignature)
        {
            DeviceId = deviceId;
            Token = sharedAccessSignature;
        }

        /// <summary>
        /// Name of device.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Shared access signature generated using device's shared access key.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Populates the builder with values needed to authenticate with device's shared access signature.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">The connection build object to populate.</param>
        /// <returns>The populated connection string builder object.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKey = null;
            iotHubConnectionStringBuilder.DeviceId = DeviceId;
            iotHubConnectionStringBuilder.SharedAccessSignature = Token;

            return iotHubConnectionStringBuilder;
        }
    }
}
