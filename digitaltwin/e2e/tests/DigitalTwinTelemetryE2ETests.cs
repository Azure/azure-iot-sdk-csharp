// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.E2ETests.interfaces;
using System;
using Xunit;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
using Microsoft.Azure.Devices.DigitalTwin.E2ETests.meta;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.DigitalTwin.E2ETests.interfaces.TestInterface2;

namespace Microsoft.Azure.Devices.DigitalTwin.E2ETests
{
    [Trait("TestCategory", "DigitalTwin")]
    [Trait("TestCategory", "E2E")]
    public class DigitalTwinTelemetryE2ETests
    {
        private static String telemetryDevicePrefix = "digitaltwine2e-telemetry";

        //wait at most 60 seconds for the sent message to be received by the Eventhub receiver
        private static int EventHubReceiveTimeoutSeconds = 60;

        public DigitalTwinTelemetryE2ETests()
        {
            EventHubListener.Instance.startListening();
        }

        public const string COMPLEX_JSON_OBJECT =
            "{" +
                "\"intergerValue\": 1," +
                "\"stringValue\": \"someString\"," +
                "\"stringArrayValue\":" +
                "[" +
                    "\"someString1\", \"someString2\", \"someString3\", \"someString4\"" +
                "]" +
            "}";

        public const string COMPLEX_JSON_VALUE =
            "{" +
                "\"x\":1234" +
            "}, " +
            "{" +
            "   \"y\":5678" +
            "}, " +
            "{" +
            "   \"z\":1010" +
            "}";

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_INTEGER, "1000")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_BOOLEAN, "false")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_DOUBLE, "1.0000000001")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_STRING, "\"someString\"")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_LONG, "9223372036854775807")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_COMPLEX_VALUE, COMPLEX_JSON_VALUE)]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_FLOAT, "1.001")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_ARRAY, "[ 100, 500, 300, 200, 400 ]")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_COMPLEX_OBJECT, COMPLEX_JSON_OBJECT)]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_ENUM, "offline")]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_MAP, "offline")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_INTEGER, "1000")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_BOOLEAN, "false")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_DOUBLE, "1.0000000001")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_STRING, "\"someString\"")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_LONG, "9223372036854775807")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_COMPLEX_VALUE, COMPLEX_JSON_VALUE)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_FLOAT, "1.001")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_ARRAY, "[ 100, 500, 300, 200, 400 ]")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_COMPLEX_OBJECT, COMPLEX_JSON_OBJECT)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_ENUM, "offline")]
        public async void sendTelemetryWithDifferentTypes(Devices.Client.TransportType transportType, string telemetryName, string telemetryValue)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(telemetryDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testcapabilitymodel:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface2 testInterface2 = new TestInterface2(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface2 }).ConfigureAwait(false);

                await testInterface2.sendTelemetryAsync(telemetryName, telemetryValue);

                string expectedEventhubPayload = "{ \"" + telemetryName + "\": " + telemetryValue + " }";
                Assert.True(EventHubListener.Instance.messageWasReceived(expectedEventhubPayload, EventHubReceiveTimeoutSeconds));
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, TestInterface2.TELEMETRY_NAME_INTEGER, "1000")]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, TestInterface2.TELEMETRY_NAME_INTEGER, "1000")]
        public async void sendTelemetryWithDifferentTypesMultithreaded(Devices.Client.TransportType transportType, string telemetryName, string telemetryValue)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(telemetryDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testcapabilitymodel:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface2 testInterface2 = new TestInterface2(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface2 }).ConfigureAwait(false);

                int MaxThreads = 5;
                List<Task> threads = new List<Task>();
                for (int threadIndex = 0; threadIndex < MaxThreads; threadIndex++)
                {
                    //intentionally not awaiting, want to send multiple telemetry messages around the same time
                    threads.Add(testInterface2.sendTelemetryAsync(telemetryName, telemetryValue + threadIndex));
                }

                for (int threadIndex = 0; threadIndex < MaxThreads; threadIndex++)
                {
                    threads[threadIndex].Wait();
                    string expectedEventhubPayload = "{ \"" + telemetryName + "\": " + telemetryValue + threadIndex + " }";
                    Assert.True(EventHubListener.Instance.messageWasReceived(expectedEventhubPayload, EventHubReceiveTimeoutSeconds));
                }
            }
        }
    }
}
