// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.E2ETests.interfaces;
using Microsoft.Azure.Devices.DigitalTwin.Service;
using Microsoft.Azure.Devices.Client;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Azure;

namespace Microsoft.Azure.Devices.DigitalTwin.E2ETests
{
    [Trait("TestCategory", "E2E")]
    [Trait("TestCategory", "PnP")]
    public class DigitalTwinPropertyE2ETests
    {
        private const string propertiesDevicePrefix = "digitaltwine2e-properties";
        private const int maxDelayForPropertyUpdateInSeconds = 10;

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task testUpdateSingleWritablePropertySuccessAsync(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyName = "writableProperty";
                string propertyValue = "someString";

                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<string> result = await updateProperty(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue);
                string returnedPayload = result.Value;

                string expected = "\"" + propertyName + "\":{\"desired\":{\"value\":\"" + propertyValue + "\"}}";
                Assert.Contains(expected, returnedPayload);

                Assert.True(testInterface.propertyWasUpdated(propertyName, propertyValue, maxDelayForPropertyUpdateInSeconds), "Expected property " + propertyName + " to be updated with value " + propertyValue);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async void testUpdateMultipleWritablePropertiesOnSingleInterfacesSuccess(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyName = "writableProperty";
                string propertyValue = "someString";
                string property2Name = "anotherWritableProperty";
                string property2Value = "someOtherString";

                TestInterface2 testInterface2 = new TestInterface2(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface2 }).ConfigureAwait(false);

                string result = await this.updateProperties(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue, property2Name, property2Value);

                string expectedResultSubstring = "\"" + propertyName + "\":{\"desired\":{\"value\":\"" + propertyValue + "\"}}";
                string expectedResultSubstring2 = "\"" + property2Name + "\":{\"desired\":{\"value\":\"" + property2Value + "\"}}";
                Assert.Contains(expectedResultSubstring, result);
                Assert.Contains(expectedResultSubstring2, result);
                Assert.True(testInterface2.propertyWasUpdated(propertyName, propertyValue, maxDelayForPropertyUpdateInSeconds), "Expected property " + propertyName + " to be updated with value " + propertyValue);
                Assert.True(testInterface2.propertyWasUpdated(property2Name, property2Value, maxDelayForPropertyUpdateInSeconds), "Expected property " + property2Name + " to be updated with value " + property2Value);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async void testUpdateSingleWritablePropertyOnSingleInterfacesMultithreadedSuccess(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyName = "writableProperty";
                string propertyValue = "someString";

                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                int MaxThreads = 5;
                List<Task<Response<string>>> threads = new List<Task<Response<string>>>();
                for (int threadIndex = 0; threadIndex < MaxThreads; threadIndex++)
                {
                    //intentionally not awaiting, want to update the same property multiple times around the same time
                    threads.Add(updateProperty(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue + threadIndex));
                }

                for (int threadIndex = 0; threadIndex < MaxThreads; threadIndex++)
                {
                    threads[threadIndex].Wait();
                    string result = threads[threadIndex].Result;
                    string expectedResultSubstring = "\"" + propertyName + "\":{\"desired\":{\"value\":\"" + propertyValue + threadIndex + "\"}}";
                    Assert.Contains(expectedResultSubstring, result);
                    Assert.True(testInterface.propertyWasUpdated(propertyName, propertyValue + threadIndex, maxDelayForPropertyUpdateInSeconds), "Expected property " + propertyName + " to be updated with value " + propertyValue + threadIndex);
                }
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task deviceClientClosingPreventsPropertyUpdateCallbacks(Devices.Client.TransportType transportType)
        {
            TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType);
            DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

            string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
            string interfaceInstanceName = "testInterfaceInstanceName";
            string propertyName = "writableProperty";
            string propertyValue = "someString";
            TestInterface testInterface = new TestInterface(interfaceInstanceName);
            await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

            //closes the underlying device client
            digitalTwinDevice.Dispose();

            await Assert.ThrowsAsync<HttpOperationException>(async () => await updateProperty(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue));

            Assert.False(testInterface.propertyWasUpdated(propertyName, propertyValue, maxDelayForPropertyUpdateInSeconds), "Did not expected property " + propertyName + " to be updated with value " + propertyValue);
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task interfaceClientCanReportReadOnlyProperty(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyValue = "\"someReadOnlyPropertyValue\"";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                await testInterface.updateReadOnlyPropertyAsync(propertyValue);

                DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string digitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                Assert.Contains(propertyValue, digitalTwin);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task interfaceClientCanReportReadWriteProperty(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyValue = "\"someReadWritePropertyValue\"";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                await testInterface.updateReadWritePropertyAsync(propertyValue);

                DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string digitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                Assert.Contains(propertyValue, digitalTwin);
            }
        }

        [Theory]
        [InlineData(Devices.Client.TransportType.Mqtt_Tcp_Only)]
        [InlineData(Devices.Client.TransportType.Mqtt_WebSocket_Only)]
        public async Task interfaceClientCanReportMultipleProperties(Devices.Client.TransportType transportType)
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string property1Name = TestInterface.ReadWritePropertyName;
                string property1Value = "\"property1Value\"";
                string property2Name = TestInterface.AnotherWritablePropertyName;
                string property2Value = "\"property2Value\"";
                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                await testInterface.updateMultiplePropertiesAsync(property1Name, property1Value, property2Name, property2Value);

                DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
                string digitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);
                string property1Substring = "\"" + property1Name + "\":{\"reported\":{\"value\":" + property1Value + "}}";
                string property2Substring = "\"" + property2Name + "\":{\"reported\":{\"value\":" + property2Value + "}}";
                Assert.Contains(property1Substring, digitalTwin);
                Assert.Contains(property2Substring, digitalTwin);
            }
        }

        [Fact(Skip = "Service isn't preventing undefined properties from being set right now")]
        public async void testUpdateWritablePropertyUnknownProperty()
        {
            Devices.Client.TransportType transportType = Devices.Client.TransportType.Mqtt_Tcp_Only;
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyName = "undefinedProperty";
                string propertyValue = "1234";

                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<string> result = await updateProperty(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue);
                string returnedPayload = result.Value;

                string expected = "\"" + propertyName + "\":{\"desired\":{\"value\":\"" + propertyValue + "\"}}";
                Assert.Contains(expected, returnedPayload);

                Assert.True(testInterface.propertyWasUpdated(propertyName, propertyValue, maxDelayForPropertyUpdateInSeconds), "Expected property " + propertyName + " to be updated with value " + propertyValue);
            }
        }

        [Fact(Skip = "Service isn't preventing read only properties from being updated right now")]
        public async Task testUpdateSingleReadonlyPropertyFails()
        {
            Devices.Client.TransportType transportType = Devices.Client.TransportType.Mqtt_Tcp_Only;
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(propertiesDevicePrefix, transportType))
            {
                DigitalTwinClient digitalTwinClient = digitalTwinDevice.digitalTwinClient;

                string capabilityModelId = "urn:contoso:azureiot:sdk:testinterface:cm:1";
                string interfaceInstanceName = "testInterfaceInstanceName";
                string propertyName = "readOnlyProperty";
                string propertyValue = "1234";

                TestInterface testInterface = new TestInterface(interfaceInstanceName);
                await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { new DeviceInformationInterface(), testInterface }).ConfigureAwait(false);

                Response<string> result = await updateProperty(digitalTwinDevice.digitalTwinId, interfaceInstanceName, propertyName, propertyValue);
                string returnedPayload = result.Value;
            }
        }

        private async Task<Response<string>> updateProperty(string digitalTwinId, string interfaceInstanceName, string propertyName, string propertyValue)
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);

            string patch =
                "{" +
                "  \"properties\": {" +
                "    \"" + propertyName + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + propertyValue + "\"" +
                "      }" +
                "    }" +
                "  }" +
                "}";

            return await digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch);
        }

        private async Task<Response<string>> updateProperties(string digitalTwinId, string interfaceInstanceName, string propertyName, string propertyValue, string property2Name, string property2Value)
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);

            string patch =
                "{" +
                "  \"properties\": {" +
                "    \"" + propertyName + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + propertyValue + "\"" +
                "      }" +
                "    }," +
                "    \"" + property2Name + "\": {" +
                "      \"desired\": {" +
                "        \"value\": \"" + property2Value + "\"" +
                "      }" +
                "    }" +
                "  }" +
                "}";

            return await digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch);
        }
    }
}
