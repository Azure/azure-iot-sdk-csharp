// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using CommandLine;
using System;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    public enum TransportType
    {
        Mqtt,
        Amqp,
    };

    public enum JsonSerializingLibrary
    {
        SystemTextJson,
        NewtonsoftJson,
    };

    internal class Parameters
    {
        [Option(
            'c',
            "ConnectionString",
            Required = false,
            HelpText = "The connection string for the device to simulate.")]
        public string ConnectionString { get; set; } = Environment.GetEnvironmentVariable("IOTHUB_LONG_HAUL_DEVICE_CONNECTION_STRING");

        [Option(
            'i',
            "InstrumentationKey",
            Required = false,
            HelpText = "The instrumentation key string for application insights.")]
        public string InstrumentationKey { get; set; } = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_INSTRUMENTATION_KEY");

        [Option(
            't',
            "Transport",
            Default = TransportType.Mqtt,
            Required = false,
            HelpText = "The transport to use for the connection (i.e., Mqtt, Amqp).")]
        public TransportType Transport { get; set; }

        [Option(
            'p',
            "TransportProtocol",
            Default = IotHubClientTransportProtocol.Tcp,
            Required = false,
            HelpText = "The protocol over which a transport (i.e., MQTT, AMQP) communicates.")]
        public IotHubClientTransportProtocol TransportProtocol { get; set; }

        [Option(
            'j',
            "JsonSerializer",
            Default = JsonSerializingLibrary.SystemTextJson,
            Required = false,
            HelpText = "Which JSON serialization library for the device app to use (i.e., SystemTextJson, NewtonsoftJson)")]
        public JsonSerializingLibrary JsonSerializer { get; set; }

        internal PayloadConvention GetPayloadConvention()
        {
            return JsonSerializer switch
            {
                JsonSerializingLibrary.SystemTextJson => SystemTextJsonPayloadConvention.Instance,
                JsonSerializingLibrary.NewtonsoftJson => DefaultPayloadConvention.Instance,
                _ => throw new InvalidOperationException($"Unexpected value for {JsonSerializer}."),
            };
        }

        internal IotHubClientTransportSettings GetTransportSettings()
        {
            return Transport switch
            {
                TransportType.Mqtt => new IotHubClientMqttSettings(TransportProtocol),
                TransportType.Amqp => new IotHubClientAmqpSettings(TransportProtocol),
                _ => throw new InvalidOperationException($"Unsupported transport type {Transport}/{TransportProtocol}"),
            };
        }
    }
}
