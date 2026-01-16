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
        HelpText = "Directory to save certificate and key files.")]
    public string OutputDir { get; set; } = null!;

    [Option(
        'i',
        "idScope",
        Required = true,
        HelpText = "The ID Scope of the DPS instance.")]
    public string IdScope { get; set; } = null!;

    [Option(
        'k',
        "sasKey",
        Required = true,
        HelpText = "The DPS SAS key for symmetric key authentication (enrollment group primary key).")]
    public string SasKey { get; set; } = null!;

    [Option(
        'd',
        "deviceName",
        Default = "test-device",
        HelpText = "The device registration ID / device name.")]
    public string DeviceName { get; set; } = "test-device";

    [Option(
        'p',
        "provisioningHost",
        Default = "global.azure-devices-provisioning.net",
        HelpText = "The DPS global provisioning host.")]
    public string ProvisioningHost { get; set; } = "global.azure-devices-provisioning.net";

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
        HelpText = "The transport to use to communicate with IoT Hub (Mqtt_Tcp_Only is required for certificate renewal).")]
    public TransportType TransportType { get; set; } = TransportType.Mqtt_Tcp_Only;
}
