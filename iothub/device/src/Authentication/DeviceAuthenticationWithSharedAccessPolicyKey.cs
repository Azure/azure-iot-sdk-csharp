// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Extensions;
using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access policy key.
    /// </summary>
    public sealed class DeviceAuthenticationWithSharedAccessPolicyKey : IAuthenticationMethod
    {
        private string _deviceId;
        private string _policyName;
        private string _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithSharedAccessPolicyKey"/> class.
        /// </summary>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="policyName">Name of the shared access policy to use.</param>
        /// <param name="key">Key associated with the shared access policy.</param>
        public DeviceAuthenticationWithSharedAccessPolicyKey(string deviceId, string policyName, string key)
        {
            SetDeviceId(deviceId);
            SetKey(key);
            SetPolicyName(policyName);
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
        /// Gets or sets the key associated with the shared policy.
        /// </summary>
        public string Key
        {
            get => _key;
            set => SetKey(value);
        }

        /// <summary>
        /// Name of the shared access policy.
        /// </summary>
        public string PolicyName
        {
            get => _policyName;
            set => SetPolicyName(value);
        }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionStringBuilder"/> instance.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.DeviceId = DeviceId;
            iotHubConnectionStringBuilder.SharedAccessKey = Key;
            iotHubConnectionStringBuilder.SharedAccessKeyName = PolicyName;
            iotHubConnectionStringBuilder.SharedAccessSignature = null;

            return iotHubConnectionStringBuilder;
        }

        private void SetDeviceId(string deviceId)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            _deviceId = deviceId;
        }

        private void SetKey(string key)
        {
            if (key.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (!StringValidationHelper.IsBase64String(key))
            {
                throw new ArgumentException("Key must be base64 encoded");
            }

            _key = key;
        }

        private void SetPolicyName(string policyName)
        {
            if (policyName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(policyName));
            }

            _policyName = policyName;
        }
    }
}
