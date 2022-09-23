// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices;

namespace AzureSasCredentialAuthenticationSample
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        /// <summary>
        /// The resource that the shared access token should grant access to. For cases where the token will be used for 
        /// more than one function(i.e.used by registryManager to create a device and used by serviceClient to send cloud 
        /// to device messages), this value should be the hostName of your IoT hub ("my-azure-iot-hub.azure-devices.net"
        /// for example). Shared access signatures do support scoping of the resource authorization by making this resourceUri
        /// more specific.For example, a resourceUri of "my-azure-iot-hub.azure-devices.net/devices" will make this token only usable
        /// when creating/updating/deleting device identities. 
        /// For more examples, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-security#use-security-tokens-from-service-components"/>
        /// </summary>
        [Option(
            'r',
            "ResourceUri",
            Required = true,
            HelpText = "The resource that the shared access token should grant access to. Ex: my-iot-hub.azure-devices.net")]

        public string ResourceUri { get; set; }

        [Option(
           'd',
           "DeviceId",
           Required = true,
           HelpText = "The IoT hub device to send a message to.")]

        public string DeviceId { get; set; }

        [Option(
            "Protocol",
            Default = IotHubTransportProtocol.Tcp,
            Required = false,
            HelpText = "The protocol to use to communicate with the IoT hub.")]
        
        public IotHubTransportProtocol Protocol { get; set; }

        [Option(
            's',
            "SharedAccessKey",
            Required = true,
            HelpText = "The shared access key for connecting to IoT hub.")]
        public string SharedAccessKey { get; set; }

        [Option(
            'n',
            "SharedAccessKeyName",
            Required = true,
            HelpText = "The shared access key name for the access key supplied. Eg. iothubowner, registryRead etc.")]
        public string SharedAccessKeyName { get; set; }
    }
}
