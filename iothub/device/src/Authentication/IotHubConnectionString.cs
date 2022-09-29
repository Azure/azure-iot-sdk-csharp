// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A data object that holds the different components that make up a device/module specific connection string.
    /// </summary>
    internal class IotHubConnectionString
    {
        internal IotHubConnectionString(
            string iotHubHostName,
            string gatewayHostName,
            string deviceId,
            string moduleId,
            string sharedAccessKeyName,
            string sharedAccessKey,
            string sharedAccessSignature)
        {
            IotHubHostName = iotHubHostName;
            GatewayHostName = gatewayHostName;
            DeviceId = deviceId;
            ModuleId = moduleId;
            SharedAccessKeyName = sharedAccessKeyName;
            SharedAccessKey = sharedAccessKey;
            SharedAccessSignature = sharedAccessSignature;
        }

        /// <summary>
        /// The value of the fully-qualified DNS host name of the IoT hub service.
        /// </summary>
        public string IotHubHostName { get; }

        /// <summary>
        /// The optional name of the gateway service to connect to.
        /// </summary>
        public string GatewayHostName { get; }

        /// <summary>
        /// The device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// The module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// The shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        public string SharedAccessKeyName { get; }

        /// <summary>
        /// The shared access key used to connect to the IoT hub service.
        /// </summary>
        public string SharedAccessKey { get; }

        /// <summary>
        /// The shared access signature used to connect to the IoT hub service.
        /// </summary>
        /// <remarks>
        /// This is used when a device app creates its own limited-lifespan SAS token, instead of letting
        /// this SDK derive one from a shared access token. When a device client is initialized with a
        /// SAS token, when that token expires, the client must be disposed, and if desired, recreated
        /// with a newly derived SAS token.
        /// </remarks>
        public string SharedAccessSignature { get; }

        /// <summary>
        /// Produces the connection string based on the values of the instance properties.
        /// </summary>
        /// <returns>A properly formatted connection string.</returns>
        public override sealed string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.HostNamePropertyName, IotHubHostName);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.DeviceIdPropertyName, DeviceId);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.ModuleIdPropertyName, ModuleId);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessKeyPropertyName, SharedAccessKey);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.SharedAccessSignaturePropertyName, SharedAccessSignature);
            sb.AppendKeyValuePairIfNotEmpty(IotHubConnectionStringConstants.GatewayHostNamePropertyName, GatewayHostName);
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
    }
}