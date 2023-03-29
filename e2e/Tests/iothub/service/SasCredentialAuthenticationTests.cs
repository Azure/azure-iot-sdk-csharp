// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// Tests to ensure authentication using SAS credential succeeds in all the clients.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    public class SasCredentialAuthenticationTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(SasCredentialAuthenticationTests)}_";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task RegistryManager_Http_SasCredentialAuth_Success()
        {
            // arrange
            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            var device = new Device(Guid.NewGuid().ToString());

            // act
            Device createdDevice = await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);

            // assert
            Assert.IsNotNull(createdDevice);

            // cleanup
            await serviceClient.Devices.DeleteAsync(device.Id).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task RegistryManager_Http_SasCredentialAuth_Renewed_Success()
        {
            // arrange
            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(-1));
            var sasCredential = new AzureSasCredential(signature);
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.GetIotHubHostName(),
                sasCredential);

            var device = new Device(Guid.NewGuid().ToString());

            // act
            try
            {
                await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);
                Assert.Fail("The SAS token is expired so the call should fail with an exception");
            }
            catch (IotHubServiceException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized)
            {
                // Expected to be unauthorized exception.
            }
            signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            sasCredential.Update(signature);
            Device createdDevice = await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);

            // assert
            Assert.IsNotNull(createdDevice);

            // cleanup
            await serviceClient.Devices.DeleteAsync(device.Id).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task JobClient_Http_SasCredentialAuth_Success()
        {
            // arrange
            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.GetIotHubHostName(), new AzureSasCredential(signature));

            string jobId = "JOBSAMPLE" + Guid.NewGuid().ToString();
            string jobDeviceId = "JobsSample_Device";
            string query = $"DeviceId IN ['{jobDeviceId}']";
            var twin = new ClientTwin(jobDeviceId);

            try
            {
                // act
                var scheduledTwinUpdateOptions = new ScheduledJobsOptions
                {
                    JobId = jobId,
                    MaxExecutionTime = TimeSpan.FromMinutes(2),
                };
                TwinScheduledJob scheduledJob = await serviceClient.ScheduledJobs
                    .ScheduleTwinUpdateAsync(
                        query,
                        twin,
                        DateTimeOffset.UtcNow,
                        scheduledTwinUpdateOptions)
                    .ConfigureAwait(false);
            }
            catch (IotHubServiceException ex) when (ex.StatusCode is (HttpStatusCode)429)
            {
                // Concurrent jobs can be rejected, but it still means authentication was successful. Ignore the exception.
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DigitalTwinClient_Http_SasCredentialAuth_Success()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            string thermostatModelId = "dtmi:com:example:TemperatureController;1";

            // Create a device client instance initializing it with the "Thermostat" model.
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = thermostatModelId,
            };
            // Call openAsync() to open the device's connection, so that the ModelId is sent over Mqtt CONNECT packet.
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            // act
            DigitalTwinGetResponse<ThermostatTwin> response = await serviceClient.DigitalTwins
                .GetAsync<ThermostatTwin>(testDevice.Id)
                .ConfigureAwait(false);

            ThermostatTwin twin = response.DigitalTwin;
            // assert
            twin.Metadata.ModelId.Should().Be(thermostatModelId);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Service_Amqp_SasCredentialAuth_Success()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.GetIotHubHostName(),
                new AzureSasCredential(signature));

            // act

            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            var message = new OutgoingMessage("Hello, Cloud!");

            await serviceClient.Messages.SendAsync(testDevice.Id, message);

            // cleanup
            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Service_Amqp_SasCredentialAuth_Renewed_Success()
        {
            // arrange
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            string signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(-1));
            var sasCredential = new AzureSasCredential(signature);
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.GetIotHubHostName(),
                sasCredential);

            // act
            try
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                Assert.Fail("The SAS token is expired so the call should fail with an exception");
            }
            catch (AmqpException ex) when (ex.Error.Description.Contains("401"))
            {
                // Expected to get an unauthorized exception.
            }

            signature = TestConfiguration.IotHub.GetIotHubSharedAccessSignature(TimeSpan.FromHours(1));
            sasCredential.Update(signature);

            await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            var message = new OutgoingMessage("Hello, Cloud!");
            await serviceClient.Messages.SendAsync(testDevice.Id, message);
            await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
        }
    }
}
