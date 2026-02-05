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
        HelpText = "The device name (used to locate credential files).")]
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
        HelpText = "The transport to use to communicate with IoT Hub (Mqtt_Tcp_Only is required for certificate renewal).")]
    public TransportType TransportType { get; set; } = TransportType.Mqtt_Tcp_Only;
}
