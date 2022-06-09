﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 primary and secondary certificates.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate. It can receive primary and secondary
    /// certificate, but only the primary is mandatory.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <c>
    ///  {
    ///      "primary": {
    ///          "certificate": "-----BEGIN CERTIFICATE-----\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "-----END CERTIFICATE-----\n";
    ///      },
    ///      "secondary": {
    ///          "certificate": "-----BEGIN CERTIFICATE-----\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n" +
    ///                         "-----END CERTIFICATE-----\n";
    ///      }
    ///  }
    /// </c>
    ///
    /// After send an X509 certificate with success, the provisioning service will return the <see cref="X509CertificateInfo"/>
    /// for both primary and secondary certificate. User can get these info from this class, and once again, only
    /// the primary info is mandatory. The following JSON is an example what info the provisioning service will
    /// return for X509.
    /// <c>
    ///  {
    ///      "primary": {
    ///          "info": {
    ///               "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "sha1Thumbprint": "0000000000000000000000000000000000",
    ///               "sha256Thumbprint": "validEnrollmentGroupId",
    ///               "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///               "notAfterUtc": "2017-11-20T12:34:18Z",
    ///               "serialNumber": "000000000000000000",
    ///               "version": 3
    ///           }
    ///      },
    ///      "secondary": {
    ///          "info": {
    ///               "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "sha1Thumbprint": "0000000000000000000000000000000000",
    ///               "sha256Thumbprint": "validEnrollmentGroupId",
    ///               "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///               "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///               "notAfterUtc": "2017-11-20T12:34:18Z",
    ///               "serialNumber": "000000000000000000",
    ///               "version": 3
    ///           }
    ///      }
    ///  }
    /// </c>
    /// </example>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Public API cannot change name.")]
    public class X509Certificates
    {
        internal X509Certificates(X509Certificate2 primary, X509Certificate2 secondary = null)
        {
            Primary = new X509CertificateWithInfo(primary);

            Secondary = secondary == null
                ? null
                : new X509CertificateWithInfo(secondary);
        }

        internal X509Certificates(string primary, string secondary = null)
        {
            Primary = new X509CertificateWithInfo(primary);

            Secondary = secondary == null
                ? null
                : new X509CertificateWithInfo(secondary);
        }

        [JsonConstructor]
        private X509Certificates(X509CertificateWithInfo primary, X509CertificateWithInfo secondary = null)
        {
            Primary = primary ?? throw new ProvisioningServiceClientException("Primary certificate cannot be null.");
            Secondary = secondary;
        }

        /// <summary>
        /// Primary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "primary")]
        public X509CertificateWithInfo Primary { get; private set; }

        /// <summary>
        /// Secondary certificate.
        /// </summary>
        [JsonProperty(PropertyName = "secondary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public X509CertificateWithInfo Secondary { get; private set; }
    }
}
