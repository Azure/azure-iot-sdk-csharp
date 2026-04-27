// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

string? deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_DEVICE_CONNECTION_STRING");

if (string.IsNullOrEmpty(deviceConnectionString) || !deviceConnectionString.Contains(".device.azure-devices."))
{
    Console.WriteLine("Must set environment variable \"IOTHUB_DEVICE_CONNECTION_STRING\" with value that matches the pattern \"<hub name>.device.azure-devices.<dnsSuffix>\". Endpoints with connection strings that look like \"<hub name>.azure-devices.<dnsSuffix>\" do not support TLS 1.3");
    return -1;
}

// This sample explicitly sets the minimum TLS version to 1.3 for reference purposes only. Without setting this, the SDK should still connect using TLS 1.3 when available since it is the newest available supported version.
TlsVersions.Instance.SetMinimumTlsVersions(System.Security.Authentication.SslProtocols.Tls13);

// All protocols (AMQP, AMQP_WS, MQTT, MQTT_WS, and HTTP) support TLS 1.3 currently
using DeviceClient client = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp_Tcp_Only);

Console.WriteLine("Opening TLS 1.3 connection");
await client.OpenAsync();

Console.WriteLine("Sending telemetry on TLS 1.3 connection");
using Message msg = new Message(Encoding.UTF8.GetBytes("Hello from TLS 1.3 sample!"));
await client.SendEventAsync(msg);

Console.WriteLine("Closing client");
await client.CloseAsync();

return 0;