// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

string? serviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_SERVICE_CONNECTION_STRING");

if (string.IsNullOrEmpty(serviceConnectionString) || !serviceConnectionString.Contains(".service.azure-devices."))
{
    Console.WriteLine("Must set environment variable \"IOTHUB_SERVICE_CONNECTION_STRING\" with value that matches the pattern \"<hub name>.service.azure-devices.<dnsSuffix>\". Endpoints with connection strings that look like \"<hub name>.azure-devices.<dnsSuffix>\" do not support TLS 1.3");
    return -1;
}

// This sample explicitly sets the minimum TLS version to 1.3 for reference purposes only. Without setting this, the SDK should still connect using TLS 1.3 when available since it is the newest available supported version.
TlsVersions.Instance.SetMinimumTlsVersions(System.Security.Authentication.SslProtocols.Tls13);

using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(serviceConnectionString);

string deviceId = Guid.NewGuid().ToString();
Device deviceToCreate = new Device(deviceId);

Console.WriteLine("Creating device over HTTP connection");
await registryManager.AddDeviceAsync(deviceToCreate);

using ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString, TransportType.Amqp);

Console.WriteLine("Sending message over AMQP connection");
using Message msg = new Message(Encoding.UTF8.GetBytes("Hello from TLS 1.3 sample!"));
await serviceClient.SendAsync(deviceId, msg);

Console.WriteLine("Closing service client");
await serviceClient.CloseAsync();

return 0;