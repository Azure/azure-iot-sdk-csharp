// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.IoT.DigitalTwin.Device;
using Microsoft.Azure.IoT.DigitalTwin.E2ETests.interfaces;
using Microsoft.Azure.IoT.DigitalTwin.E2ETests.meta;
using Microsoft.Azure.IoT.DigitalTwin.Service;
using Microsoft.Azure.Devices.Client;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.IoT.DigitalTwin.E2ETests
{
    public class DigitalTwinRegisterInterfacesE2ETests
    {
        private static String registerInterfacesDevicePrefix = "digitaltwine2e-registerinterfaces";

        //wait at most 60 seconds for the sent message to be received by the eventhub receiver
        private static int EventHubReceiveTimeoutSeconds = 60;

        public DigitalTwinRegisterInterfacesE2ETests()
        {
            //constructor is run before each test, but multiple calls to this method are allowed
            EventHubListener.Instance.startListening();
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelWithSingleInterface(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                string expectedCapabilityModelString = "{ \"capabilityModelId\": \"" + capabilityModelId + "\"";
                Assert.True(EventHubListener.Instance.messageContainingSubstringsWasReceived(EventHubReceiveTimeoutSeconds, new string[] { expectedCapabilityModelString, TestInterface.InterfaceId }), "Event hub never received the device registration message");

                Assert.True(testInterface.onRegistrationCompleteExecuted, "OnRegistrationComplete was not executed");

                //verify that service client can see the newly registered interface
                var digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string serviceClientDigitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                Assert.Contains(TestInterface.InterfaceId, serviceClientDigitalTwin);
            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelWithMultipleInterfaces(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:2";
                String interfaceInstanceName = "testInterfaceInstanceName";
                String interfaceInstance2Name = "testInterfaceInstance2Name";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                TestInterface2 testInterface2 = new TestInterface2(interfaceInstance2Name);

                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface, testInterface2 }).ConfigureAwait(false);

                string expectedCapabilityModelString = "{ \"capabilityModelId\": \"" + capabilityModelId + "\"";

                Assert.True(testInterface.onRegistrationCompleteExecuted, "OnRegistrationComplete never executed");
                Assert.True(testInterface2.onRegistrationCompleteExecuted, "OnRegistrationComplete never executed");

                //verify that service client can see the newly registered interfaces
                var digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string serviceClientDigitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                Assert.Contains(TestInterface.InterfaceId, serviceClientDigitalTwin);
                Assert.Contains(TestInterface2.InterfaceId, serviceClientDigitalTwin);
            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelMultipleTimes(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String capabilityModel2Id = "urn:contoso:azureiot:sdk:testinterface:cm:2";
                String interfaceInstanceName = "testInterfaceInstanceName";
                String interfaceInstance2Name = "testInterfaceInstance2Name";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                TestInterface2 testInterface2 = new TestInterface2(interfaceInstance2Name);

                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                bool exceptionThrown = false;
                try
                {
                    await digitalTwinClient.RegisterInterfacesAsync(capabilityModel2Id, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface, testInterface2 }).ConfigureAwait(false);
                }
                catch (InvalidOperationException e)
                {
                    //This operation should throw an InvalidOperationException due to registerInterfacesAsync preventing multiple calls
                    exceptionThrown = true;
                }

                Assert.True(exceptionThrown, "No exception was thrown even though register was called multiple times");
                Assert.True(testInterface.onRegistrationCompleteExecuted, "No registration complete callback was executed when 1 was expected");            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelMultipleTimesMultithreaded(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                String capabilityModel2Id = "urn:contoso:azureiot:sdk:testinterface:cm:2";
                String interfaceInstanceName = "testInterfaceInstanceName";
                String interfaceInstance2Name = "testInterfaceInstance2Name";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                TestInterface2 testInterface2 = new TestInterface2(interfaceInstance2Name);

                var registerInterfacesTask = digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);
                var registerInterfacesTask2 = digitalTwinClient.RegisterInterfacesAsync(capabilityModel2Id, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface, testInterface2 }).ConfigureAwait(false);

                bool firstTaskThrewException = false;
                bool secondTaskThrewException = false;

                try
                {
                    await registerInterfacesTask;
                }
                catch (InvalidOperationException e)
                {
                    firstTaskThrewException = true;
                }

                try
                {
                    await registerInterfacesTask2;
                }
                catch (InvalidOperationException e)
                {
                    //One of these threads should throw an InvalidOperationException due to registerInterfacesAsync preventing multiple calls
                    secondTaskThrewException = true;
                }

                Assert.False(firstTaskThrewException, "Expected first task to succeed since it started first");
                Assert.True(secondTaskThrewException, "No exception was thrown even though register was called multiple times");
                Assert.True(testInterface.onRegistrationCompleteExecuted, "No registration complete callback was executed when 1 was expected");
            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelWithoutAnyInterfacesThrows(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;
                String capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";

                bool exceptionThrown = false;
                try
                {
                    await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { }).ConfigureAwait(false);
                }
                catch (InvalidOperationException e)
                {
                    //One of these threads should throw an InvalidOperationException due to registerInterfacesAsync preventing multiple calls
                    exceptionThrown = true;
                }

                Assert.True(exceptionThrown, "No exception was thrown during registration even though no interfaces were provided");
            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelWhenDeviceClientAlreadyOpen(TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                //Open call is okay to do before registerInterfacesAsync. The open call in the registerInterfacesAsync call
                // will just return immediately since the client is already open
                await digitalTwinDevice.deviceClient.OpenAsync();

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                string expectedCapabilityModelString = "{ \"capabilityModelId\": \"" + capabilityModelId + "\"";
                Assert.True(EventHubListener.Instance.messageContainingSubstringsWasReceived(EventHubReceiveTimeoutSeconds, new string[] { expectedCapabilityModelString, TestInterface.InterfaceId }), "Eventhub never received the device registration message");

                Assert.True(testInterface.onRegistrationCompleteExecuted, "Digital twin client never executed callback for on registration complete");

                //verify that service client can see the newly registered interface
                var digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string serviceClientDigitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                Assert.Contains(TestInterface.InterfaceId, serviceClientDigitalTwin);
            }
        }

        [Theory]
        [InlineData(TransportType.Mqtt_Tcp_Only)]
        [InlineData(TransportType.Mqtt_WebSocket_Only)]
        public async void registerCapbilityModelWhenDeviceClientIsDisposed(TransportType transportType)
        {
            TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(registerInterfacesDevicePrefix, transportType);
            
            DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

            await digitalTwinDevice.deviceClient.OpenAsync();
            await digitalTwinDevice.deviceClient.CloseAsync();

            string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
            string interfaceInstanceName = "testInterfaceInstanceName";
            TestInterface testInterface = new TestInterface(interfaceInstanceName);

            bool exceptionThrown = false;
            try
            {
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);
            }
            catch (ObjectDisposedException e)
            {
                //Device client is disposed when it is closed, so any operations using the device client
                // should throw an object disposed exception
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown, "No exception was thrown during registration even though underlying device client was disposed");
        }
    }
}
