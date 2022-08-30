// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Authentication method that uses a device's shared access key to authenticate with service.
    /// </summary>
    public class ServiceAuthenticationWithDeviceSharedAccessPolicyKey : IAuthenticationMethod
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="sharedAccessKey">The shared access policy value.</param>
        public ServiceAuthenticationWithDeviceSharedAccessPolicyKey(string deviceId, string sharedAccessKey)
        {
            DeviceId = deviceId;
            Key = sharedAccessKey;
        }

        /// <summary>
        /// Shared access key of the device.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Name of device.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Populates the builder with values needed to authenticate with device's shared access key.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">The connection build object to populate.</param>
        /// <returns>The populated connection string builder object.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKey = Key;
            iotHubConnectionStringBuilder.DeviceId = DeviceId;
            iotHubConnectionStringBuilder.SharedAccessSignature = null;

            return iotHubConnectionStringBuilder;
        }
    }
}
