// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal class IotHubConnectionString
    {
        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string HostName { get; internal set; }

        /// <summary>
        /// Gets the optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; internal set; }

        /// <summary>
        /// Gets the device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Gets the module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; internal set; }

        /// <summary>
        /// Gets the shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// Gets the shared access key used to connect to the IoT hub service.
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// Gets the shared access signature used to connect to the IoT hub service.
        /// </summary>
        /// <remarks>
        /// This is used when a device app creates its own limited-lifespan SAS token, instead of letting
        /// this SDK derive one from a shared access token. When a device client is initialized with a
        /// SAS token, when that token expires, the client must be disposed, and if desired, recreated
        /// with a newly derived SAS token.
        /// </remarks>
        public string SharedAccessSignature { get; internal set; }
    }
}
