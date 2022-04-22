// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CA1812 //False positive on this issue. Complains about no one calling the constructor, but it is called in several places

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Device registration.
    /// </summary>
    internal partial class DeviceRegistration
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistration()
        {
        }

        /// <summary>
        /// Gets or set the custom content payload.
        /// </summary>
        [JsonProperty(PropertyName = "payload", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JRaw Payload { get; set; }

        /// <summary>
        /// The certificate signing request for device client certificate in PKCS 10 format that the device provisioning service (DPS) will send to its linked certificate authority which will sign
        /// and return an X509 device identity operational client certificate to the device.
        /// DPS will register the device and client certificate thumbprint in IoT Hub and return the certificate with the public key to the IoT device.
        /// The IoT device can then use the returned client certificate along with the private key information to authenticate with IoT Hub.
        /// </summary>
        [JsonProperty(PropertyName = "clientCertificateCsr", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientCertificateSigningRequest { get; set; }
    }
}

#pragma warning restore CA1812
