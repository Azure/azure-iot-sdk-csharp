// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    /// <summary>
    /// Parameters for the CSR provisioning sample application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'i',
            "IdScope",
            Required = true,
            HelpText = "The Id Scope of the DPS instance.")]
        public string IdScope { get; set; } = string.Empty;

        [Option(
            'r',
            "RegistrationId",
            Required = true,
            HelpText = "The registration Id for the device.")]
        public string RegistrationId { get; set; } = string.Empty;

        [Option(
            'g',
            "GlobalDeviceEndpoint",
            Default = "global.azure-devices-provisioning.net",
            HelpText = "The global endpoint for devices to connect to.")]
        public string GlobalDeviceEndpoint { get; set; } = "global.azure-devices-provisioning.net";

        [Option(
            't',
            "TransportType",
            Default = TransportType.Mqtt,
            HelpText = "The transport to use to communicate with the device provisioning instance. Possible values include Mqtt, Mqtt_WebSocket_Only, Mqtt_Tcp_Only, Amqp, Amqp_WebSocket_Only, Amqp_Tcp_only, and Http1.")]
        public TransportType TransportType { get; set; }

        [Option(
            'a',
            "AuthType",
            Default = AuthenticationType.SymmetricKey,
            HelpText = "The authentication type to use for DPS registration. Possible values: SymmetricKey, X509.")]
        public AuthenticationType AuthType { get; set; }

        [Option(
            'k',
            "SymmetricKey",
            HelpText = "The symmetric key for DPS authentication (required when AuthType is SymmetricKey). Use this for individual enrollments or provide the derived device key directly.")]
        public string? SymmetricKey { get; set; }

        [Option(
            'e',
            "EnrollmentGroupKey",
            HelpText = "The enrollment group primary/secondary key. When provided, a device-specific symmetric key will be derived using HMAC-SHA256.")]
        public string? EnrollmentGroupKey { get; set; }

        [Option(
            'c',
            "X509CertPath",
            HelpText = "Path to the X.509 certificate file (PFX/PKCS12) for DPS attestation (required when AuthType is X509).")]
        public string? X509CertPath { get; set; }

        [Option(
            'w',
            "X509CertPassword",
            HelpText = "Password for the X.509 certificate file (if encrypted).")]
        public string? X509CertPassword { get; set; }

        [Option(
            "CsrKeyType",
            Default = CsrKeyType.ECC,
            HelpText = "The key type to use for CSR generation. Possible values: ECC, RSA.")]
        public CsrKeyType CsrKeyType { get; set; }

        [Option(
            "RsaKeySize",
            Default = 2048,
            HelpText = "RSA key size in bits (when CsrKeyType is RSA). Recommended: 2048 or higher.")]
        public int RsaKeySize { get; set; }

        [Option(
            'o',
            "OutputCertPath",
            Default = "issued_certificate.pem",
            HelpText = "Output file path for the issued certificate chain (PEM format).")]
        public string OutputCertPath { get; set; } = "issued_certificate.pem";

        [Option(
            "OutputKeyPath",
            Default = "private_key.pem",
            HelpText = "Output file path for the CSR private key (PEM format).")]
        public string OutputKeyPath { get; set; } = "private_key.pem";

        [Option(
            's',
            "SendTelemetry",
            Default = true,
            HelpText = "Whether to send a test telemetry message after registration.")]
        public bool SendTelemetry { get; set; }
    }

    /// <summary>
    /// Authentication type for DPS registration.
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// Symmetric key authentication.
        /// </summary>
        SymmetricKey,

        /// <summary>
        /// X.509 certificate authentication.
        /// </summary>
        X509
    }

    /// <summary>
    /// Key type for CSR generation.
    /// </summary>
    public enum CsrKeyType
    {
        /// <summary>
        /// Elliptic Curve Cryptography (ECC) key - recommended for better performance.
        /// </summary>
        ECC,

        /// <summary>
        /// RSA key.
        /// </summary>
        RSA
    }
}
