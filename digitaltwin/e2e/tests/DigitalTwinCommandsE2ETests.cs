// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.E2ETests.interfaces;
using Microsoft.Azure.Devices.DigitalTwin.Service;
using Microsoft.Azure.Devices.Client;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DigitalTwinCommandResponse = Microsoft.Azure.Devices.DigitalTwin.Service.Models.DigitalTwinCommandResponse;
using Azure;

namespace Microsoft.Azure.Devices.DigitalTwin.E2ETests
{
    public class DigitalTwinCommandsE2ETests
    {
        private const string sampleSyncCommandName = "syncCommand";
        private const string sampleAsyncCommandName = "asyncCommand";
        private const string commandsDevicePrefix = "digitaltwine2e-commands";
        private const string sampleCommandPayload = "{\"status\":\"completed\"}";
        private const string noCommandPayload = null;

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, noCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, noCommandPayload)]
        public async Task testDeviceClientReceivesSyncCommandAndResponds(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<DigitalTwinCommandResponse> result = await invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload);
                DigitalTwinCommandResponse commandResponse = result.Value;
                Assert.NotNull(commandResponse);

                if (payload != null)
                {
                    Assert.NotNull(commandResponse);
                    Assert.NotNull(commandResponse.Payload);
                    Assert.Equal(payload, commandResponse.Payload);
                }

                Assert.Equal(DigitalTwinInterfaceClient.StatusCodeCompleted, commandResponse.Status);
                testInterface.AssertCommandCalled(commandName, commandResponse.RequestId);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleAsyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleAsyncCommandName, noCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleAsyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleAsyncCommandName, noCommandPayload)]
        public async Task testDeviceClientReceivesAsyncCommandAndResponds(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<DigitalTwinCommandResponse> result = await invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload);
                DigitalTwinCommandResponse commandResponse = result.Value;
                string returnedPayload = commandResponse.Payload;

                if (payload != null)
                {
                    Assert.NotNull(returnedPayload);
                    Assert.Equal(TestInterface.asyncCommandResponsePayload, returnedPayload);
                }

                Assert.Equal(DigitalTwinInterfaceClient.StatusCodePending, commandResponse.Status);

                testInterface.AssertCommandCalled(commandName, commandResponse.RequestId);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, noCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, noCommandPayload)]
        public async void testSameCommandNameOnMultipleRegisteredInterfacesSuccess(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:2";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                String interface2InstanceName = "testInterface2InstanceName";
                TestInterface2 testInterface2 = new TestInterface2(interface2InstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface, testInterface2 }).ConfigureAwait(false);

                Response<DigitalTwinCommandResponse> result = await invokeCommand(digitalTwinDevice.digitalTwinId, interface2InstanceName, commandName, payload);

                Assert.NotNull(result.Value);
                testInterface.AssertCommandNotCalled(commandName);
                testInterface2.AssertCommandCalled(commandName, result.Value.RequestId);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, "undefinedMethod", sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, "undefinedMethod", sampleCommandPayload)]
        public async Task testDeviceClientReceivesUnknownCommandAndResponds(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<DigitalTwinCommandResponse> result = await invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload);
                DigitalTwinCommandResponse commandResponse = result.Value;

                Assert.Equal(DigitalTwinInterfaceClient.StatusCodeNotImplemented, commandResponse.Status);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, sampleCommandPayload)]
        public async void testInvokeCommandUnknownInterfaceInstanceName(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                string unknownInterfaceInstanceName = "someInterfaceInstanceNameThatDoesNotExist";
                Response<DigitalTwinCommandResponse> result = await invokeCommand(digitalTwinDevice.digitalTwinId, unknownInterfaceInstanceName, commandName, payload);
                DigitalTwinCommandResponse commandResponse = result.Value;

                Assert.Equal(DigitalTwinInterfaceClient.StatusCodeNotImplemented, commandResponse.Status);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, "!!!!!@#!@$!R!D!@D!@$!@#!#", sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, "!!!!!@#!@$!R!D!@D!@$!@#!#", sampleCommandPayload)]
        public async void testInvokeCommandInvalidCommandName(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                await Assert.ThrowsAsync<HttpOperationException>(async () => await invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload));
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, sampleCommandPayload)]
        public async Task testInvokeCommandOnOfflineDevice(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType);
            DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

            String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
            String interfaceInstanceName = "testInterfaceInstanceName";
            TestInterface testInterface = new TestInterface(interfaceInstanceName);
            await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

            //closes the underlying device client
            digitalTwinDevice.Dispose();

            await Assert.ThrowsAsync<HttpOperationException>(async () => await invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload));
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only, sampleSyncCommandName, sampleCommandPayload)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only, sampleSyncCommandName, sampleCommandPayload)]
        public async void testInvokeCommandMultithreaded(Devices.Client.TransportType transportType, string commandName, string payload)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(commandsDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                int MaxThreads = 5;
                List<Task<Response<DigitalTwinCommandResponse>>> commandTasks = new List<Task<Response<DigitalTwinCommandResponse>>>();
                for (int threadIndex = 0; threadIndex < MaxThreads; threadIndex++)
                {
                    //intentionally not awaiting, want to invoke multiple methods around the same time
                    commandTasks.Add(invokeCommand(digitalTwinDevice.digitalTwinId, interfaceInstanceName, commandName, payload));
                }

                foreach (Task<Response<DigitalTwinCommandResponse>> commandTask in commandTasks)
                {
                    var threadResult = await commandTask.ConfigureAwait(false);
                    Assert.NotNull(threadResult.Value);
                    Assert.Equal(DigitalTwinInterfaceClient.StatusCodeCompleted, threadResult.Value.Status);
                }
            }
        }

        private async Task<Response<DigitalTwinCommandResponse>> invokeCommand(string digitalTwinId, string interfaceInstanceName, string commandName, string argument)
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
            return await digitalTwinServiceClient.InvokeCommandAsync(digitalTwinId, interfaceInstanceName, commandName, argument);
        }
    }
}
