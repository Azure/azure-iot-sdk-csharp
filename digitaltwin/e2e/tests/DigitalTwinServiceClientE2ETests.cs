// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.IoT.DigitalTwin.Service;
using Microsoft.Rest;
using System;
using Xunit;

namespace Microsoft.Azure.IoT.DigitalTwin.E2ETests
{
    public class DigitalTwinServiceClientE2ETests
    {
        private const string serviceClientTestsDevicePrefix = "digitaltwine2e-serviceclient";

        DigitalTwinServiceClient digitalTwinServiceClient;

        public DigitalTwinServiceClientE2ETests()
        {
            digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IotHubConnectionString);
        }

        [Fact]
        public async void testGetModelValidInterfaceUrn()
        {
            string modelId = "urn:contoso:azureiot:sdk:testinterface:1";
            string result = await digitalTwinServiceClient.GetModelAsync(modelId);

            Assert.Contains(modelId, result);
            Assert.Contains("\"@type\":\"Interface\"", result);
        }

        [Fact]
        public async void testGetModelValidDCMUrn()
        {
            string modelId = "urn:contoso:azureiot:sdk:testinterface:cm:2";
            string result = await digitalTwinServiceClient.GetModelAsync(modelId);

            Assert.Contains(modelId, result);
            Assert.Contains("\"@type\":\"CapabilityModel\"", result);
        }
        
        [Fact]
        public async void testGetModelInformationUnknownModelUrn()
        {
            string modelId = "urn:notreal:fakenamington:doesnotexist:nonexistentcapabilitymodel:cm:2";
            await Assert.ThrowsAsync<HttpOperationException>(async () => await digitalTwinServiceClient.GetModelAsync(modelId));
        }

        [Fact]
        public async void testGetModelInformationUnknownInterfaceUrn()
        {
            string modelId = "urn:notreal:fakenamington:doesnotexist:nonexistentinterface:2";
            await Assert.ThrowsAsync<HttpOperationException>(async () => await digitalTwinServiceClient.GetModelAsync(modelId));
        }

        [Fact]
        public async void testGetAllDigitalTwinInterfacesValidDigitalTwinId()
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(serviceClientTestsDevicePrefix, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only))
            {
                string digitalTwinString = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinDevice.digitalTwinId);

                Assert.NotNull(digitalTwinString);
                Assert.Contains("\"version\":1", digitalTwinString); //freshly created device should start at version 1
            }
        }

        [Fact]
        public async void testGetAllDigitalTwinInterfacesInvalidDigitalTwinId()
        {
            string nonExistentDigitalTwinId = "someNonExistentDigitalTwinId" + Guid.NewGuid().ToString();
            await Assert.ThrowsAsync<HttpOperationException>(async () => await digitalTwinServiceClient.GetDigitalTwinAsync(nonExistentDigitalTwinId));
        }

        [Fact]
        public async void testInvokeCommandOnInvalidDevice()
        {
            string nonExistentDigitalTwinId = "someNonExistentDigitalTwinId" + Guid.NewGuid().ToString();
            string nonExistentInterfaceInstanceName = "someNonExistentInterfaceInstanceName";
            string commandName = "someValidCommandName";
            string argument = null;
            await Assert.ThrowsAsync<HttpOperationException>(async () => await digitalTwinServiceClient.InvokeCommandAsync(nonExistentDigitalTwinId, nonExistentInterfaceInstanceName, commandName, argument));
        }

        [Fact]
        public async void testUpdatePropertyWithInvalidPatchThrows()
        {
            using (TestDigitalTwinDevice digitalTwinDevice = new TestDigitalTwinDevice(serviceClientTestsDevicePrefix, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only))
            {
                await Assert.ThrowsAsync<HttpOperationException>(async () => await digitalTwinServiceClient.UpdateDigitalTwinPropertiesAsync(digitalTwinDevice.digitalTwinId, "anyInterfaceName", "{some invalid json patch}"));
            }
        }
    }
}
