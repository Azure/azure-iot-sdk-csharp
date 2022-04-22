﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Additional parameters to be passed over registartion instance
    /// </summary>
    /// <returns>The registration result.</returns>
    public class ProvisioningRegistrationAdditionalData
    {
        /// <summary>
        /// Additional (optional) Json Data to be sent to the service
        /// </summary>
        public string JsonData { get; set; }

        /// <summary>
        /// The certificate signing request for device client certificate in PKCS 10 format that the device provisioning service (DPS) will send to its linked certificate authority which will sign
        /// and return an X509 device identity client certificate to the device.
        /// DPS will register the device and client certificate thumbprint in IoT hub and return the certificate with the public key to the IoT device.
        /// The IoT device can then use the returned client certificate along with the private key information to authenticate with IoT hub.
        /// </summary>
        public string ClientCertificateSigningRequest { get; set; }
    }
}
