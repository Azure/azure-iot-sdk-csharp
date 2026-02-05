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
        /// Gets or sets the base64-encoded Certificate Signing Request (CSR) to be sent during registration.
        /// When set, the DPS service will return an issued certificate chain in the registration result.
        /// </summary>
        /// <remarks>
        /// The CSR should be a base64-encoded DER format CSR.
        /// The Common Name (CN) in the CSR should match the registration ID.
        /// </remarks>
        public string ClientCertificateSigningRequest { get; set; }
    }
}
