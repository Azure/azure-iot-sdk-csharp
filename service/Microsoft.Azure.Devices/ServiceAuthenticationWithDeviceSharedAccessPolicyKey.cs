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
        public ServiceAuthenticationWithDeviceSharedAccessPolicyKey(string deviceId, string sharedAccessKey)
        {
            DeviceId = deviceId;
            Key = sharedAccessKey;
        }

        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKey = this.Key;
            iotHubConnectionStringBuilder.DeviceId = this.DeviceId;
            iotHubConnectionStringBuilder.SharedAccessSignature = null;

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Shared access key of the device
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Name of device
        /// </summary>
        public string DeviceId { get; set; }
    }
}