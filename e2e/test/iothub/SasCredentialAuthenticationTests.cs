// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Amqp;
using Microsoft.Rest;
using Azure;

using ClientOptions = Microsoft.Azure.Devices.Client.ClientOptions;
using Microsoft.Azure.Devices;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    /// <summary>
    /// Tests to ensure authentication using SAS credential succeeds in all the clients.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class SasCredentialAuthenticationTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(SasCredentialAuthenticationTests)}_";

        [LoggedTestMethod]
        public async Task RegistryManager_Http_SasCredentialAuth_Success()
        {
            // arrange
            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var registryClient = new RegistryClient(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            var device = new Device(Guid.NewGuid().ToString());

            // act
            Device createdDevice = await registryClient.AddDeviceAsync(device).ConfigureAwait(false);

            // assert
            Assert.IsNotNull(createdDevice);

            // cleanup
            await registryClient.RemoveDeviceAsync(device.Id).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_Http_SasCredentialAuth_Renewed_Success()
        {
            // arrange
            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(-1));
            var sasCredential = new AzureSasCredential(signature);
            using var registryClient = new RegistryClient(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                sasCredential);

            var device = new Device(Guid.NewGuid().ToString());

            // act
            try
            {
                await registryClient.AddDeviceAsync(device).ConfigureAwait(false);
                Assert.Fail("The SAS token is expired so the call should fail with an exception");
            }
            catch (UnauthorizedException)
            {
                // Expected to be unauthorized exception.
            }
            signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            sasCredential.Update(signature);
            Device createdDevice = await registryClient.AddDeviceAsync(device).ConfigureAwait(false);

            // assert
            Assert.IsNotNull(createdDevice);

            // cleanup
            await registryClient.RemoveDeviceAsync(device.Id).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task JobClient_Http_SasCredentialAuth_Success()
        {
            // arrange
            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var jobClient = JobClient.Create(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
            string jobDeviceId = "JobsSample_Device";
            string query = $"DeviceId IN ['{jobDeviceId}']";
            var twin = new Twin(jobDeviceId);

            try
            {
                // act
                JobResponse createJobResponse = await jobClient
                    .ScheduleTwinUpdateAsync(
                        jobId,
                        query,
                        twin,
                        DateTime.UtcNow,
                        (long)TimeSpan.FromMinutes(2).TotalSeconds)
                    .ConfigureAwait(false);
            }
            catch (ThrottlingException)
            {
                // Concurrent jobs can be rejected, but it still means authentication was successful. Ignore the exception.
            }
        }

        [LoggedTestMethod]
        public async Task DigitalTwinClient_Http_SasCredentialAuth_Success()
        {
            // arrange
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            string thermostatModelId = "dtmi:com:example:TemperatureController;1";

            // Create a device client instance initializing it with the "Thermostat" model.
            var options = new ClientOptions
            {
                ModelId = thermostatModelId,
            };
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt, options);

            // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
            await deviceClient.OpenAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var digitalTwinClient = DigitalTwinClient.Create(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            // act
            HttpOperationResponse<ThermostatTwin, DigitalTwinGetHeaders> response = await digitalTwinClient
                .GetDigitalTwinAsync<ThermostatTwin>(testDevice.Id)
                .ConfigureAwait(false);
            ThermostatTwin twin = response.Body;

            // assert
            twin.Metadata.ModelId.Should().Be(thermostatModelId);

            // cleanup
            await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Service_Amqp_SasCredentialAuth_Success()
        {
            // arrange
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var serviceClient = ServiceClient.Create(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                new AzureSasCredential(signature),
                TransportType.Amqp);

            // act
            await serviceClient.OpenAsync().ConfigureAwait(false);
            using var message = new Message(Encoding.ASCII.GetBytes("Hello, Cloud!"));

            await serviceClient.SendAsync(testDevice.Id, message);

            // cleanup
            await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Service_Amqp_SasCredentialAuth_Renewed_Success()
        {
            // arrange
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(Client.TransportType.Mqtt);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(-1));
            var sasCredential = new AzureSasCredential(signature);
            using var serviceClient = ServiceClient.Create(
                TestConfiguration.IoTHub.GetIotHubHostName(),
                sasCredential,
                TransportType.Amqp);

            // act
            try
            {
                await serviceClient.OpenAsync().ConfigureAwait(false);
                Assert.Fail("The SAS token is expired so the call should fail with an exception");
            }
            catch (AmqpException ex) when (ex.Error.Description.Contains("401"))
            {
                // Expected to get an unauthorized exception.
            }

            signature = TestConfiguration.IoTHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            sasCredential.Update(signature);
            await serviceClient.OpenAsync().ConfigureAwait(false);
            using var message = new Message(Encoding.ASCII.GetBytes("Hello, Cloud!"));
            await serviceClient.SendAsync(testDevice.Id, message);

            // cleanup
            await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
        }
    }
}
