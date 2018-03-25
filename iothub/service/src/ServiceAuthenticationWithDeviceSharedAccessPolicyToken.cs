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
        public ServiceAuthenticationWithDeviceSharedAccessPolicyToken(string deviceId, string sharedAccessSignature)
        {
            DeviceId = deviceId;
            Token = sharedAccessSignature;
        }

        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessKey = null;
            iotHubConnectionStringBuilder.DeviceId = this.DeviceId;
            iotHubConnectionStringBuilder.SharedAccessSignature = this.Token;

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Name of device
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Shared access signature generated using device's shared access key
        /// </summary>
        public string Token { get; set; }
    }
}
