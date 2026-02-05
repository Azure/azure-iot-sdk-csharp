// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;

namespace CertificateSigningRequestSample;

/// <summary>
/// Parameters for the Certificate Signing Request sample application.
/// </summary>
public class Parameters
{
    [Option(
        'o',
        "outputDir",
        Required = true,
        HelpText = "Directory containing certificate, key, and metadata files.")]
    public string OutputDir { get; set; } = null!;

    [Option(
        'd',
        "deviceName",
        Default = "test-device",
        HelpText = "The device name (used to locate credential files and as registration ID for DPS).")]
    public string DeviceName { get; set; } = "test-device";

    [Option(
        'm',
        "messageCount",
        Default = 3,
        HelpText = "The number of telemetry messages to send after certificate renewal.")]
    public int MessageCount { get; set; } = 3;

    [Option(
        't',
        "transportType",
        Default = TransportType.Mqtt_Tcp_Only,
        HelpText = "The transport to use (Mqtt_Tcp_Only is required for certificate renewal and DPS CSR provisioning).")]
    public TransportType TransportType { get; set; } = TransportType.Mqtt_Tcp_Only;

    // DPS Provisioning Parameters (optional - used when credentials don't exist)

    [Option(
        'i',
        "idScope",
        HelpText = "The Id Scope of the DPS instance (required for DPS provisioning when credentials don't exist).")]
    public string? IdScope { get; set; }

    [Option(
        'k',
        "symmetricKey",
        HelpText = "The symmetric key for DPS authentication. Use this for individual enrollments or provide the derived device key directly.")]
    public string? SymmetricKey { get; set; }

    [Option(
        'e',
        "enrollmentGroupKey",
        HelpText = "The enrollment group primary/secondary key. When provided, a device-specific symmetric key will be derived using HMAC-SHA256.")]
    public string? EnrollmentGroupKey { get; set; }

    [Option(
        'g',
        "globalDeviceEndpoint",
        Default = "global.azure-devices-provisioning.net",
        HelpText = "The global endpoint for devices to connect to DPS.")]
    public string GlobalDeviceEndpoint { get; set; } = "global.azure-devices-provisioning.net";

    [Option(
        "csrKeyType",
        Default = CsrKeyType.ECC,
        HelpText = "The key type to use for CSR generation when provisioning via DPS. Possible values: ECC, RSA.")]
    public CsrKeyType CsrKeyType { get; set; } = CsrKeyType.ECC;

    [Option(
        "rsaKeySize",
        Default = 2048,
        HelpText = "RSA key size in bits (when CsrKeyType is RSA). Recommended: 2048 or higher.")]
    public int RsaKeySize { get; set; } = 2048;

    /// <summary>
    /// Checks if DPS provisioning parameters are provided.
    /// </summary>
    public bool HasDpsParameters => !string.IsNullOrEmpty(IdScope) 
        && (!string.IsNullOrEmpty(SymmetricKey) || !string.IsNullOrEmpty(EnrollmentGroupKey));
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
