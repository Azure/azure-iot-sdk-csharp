// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Utilities;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses shared access policies.
    /// </summary>
    /// <remarks>
    /// The device/module connection string includes the SharedAccessKeyName and SharedAccessKey together.
    /// A use case is to use the service shared access policy with a "Device Connect" permission for the connection string.
    /// </remarks>
    public sealed class ClientAuthenticationWithSharedAccessPolicy : IAuthenticationMethod
    {
        private string _deviceId;
        private string _moduleId;
        private string _keyName;
        private string _key;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="keyName">Name of the shared access policy to use.</param>
        /// <param name="key">Key associated with the shared access policy.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="moduleId">Module identifier.</param>
        public ClientAuthenticationWithSharedAccessPolicy(string keyName, string key, string deviceId, string moduleId = default)
        {
            SetKey(key);
            SetKeyName(keyName);
            SetDeviceId(deviceId);
            SetModuleId(moduleId);
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
        /// <remarks>
        /// A sample of device connection string in this case is "HostName=[Host Name];DeviceId=[Device Name];SharedAccessKey=[Device Key];SharedAccessKeyName=[Key Name]".
        /// This property is for the field "SharedAccessKeyName".
        /// </remarks>
        public string KeyName
        {
            get => _keyName;
            set => SetKeyName(value);
        }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="iotHubConnectionCredentials"/> is null.</exception>
        public void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.SharedAccessKey = Key;
            iotHubConnectionCredentials.SharedAccessKeyName = KeyName;
            iotHubConnectionCredentials.SharedAccessSignature = null;
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

        private void SetKey(string key)
        {
            if (key.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Shared access key cannot be null or white space.");
            }

            if (!StringValidationHelper.IsBase64String(key))
            {
                throw new InvalidOperationException("Shared access key must be base64 encoded.");
            }

            _key = key;
        }

        private void SetKeyName(string keyName)
        {
            if (keyName.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Shared access key name cannot be null or white space.");
            }

            _keyName = keyName;
        }
    }
}
