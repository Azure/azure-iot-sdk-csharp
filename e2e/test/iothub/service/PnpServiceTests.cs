// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// Test class containing all tests to be run for plug and play.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Service")]
    [TestCategory("PlugAndPlay")]
    public class PnpServiceTests : E2EMsTestBase
    {
        private const string DevicePrefix = "plugAndPlayDevice";
        private const string ModulePrefix = "plugAndPlayModule";
        private const string TestModelId = "dtmi:com:example:testModel;1";

        [TestMethod]
        public async Task DeviceTwin_Contains_ModelId()
        {
            // Setup

            // Create a device.
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            // Send model ID with MQTT connect packet to make the device plug and play.
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = TestModelId,
            };
            var deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            // Act

            // Get device twin.
            ClientTwin twin = await TestDevice.ServiceClient.Twins.GetAsync(testDevice.Device.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the device was created as plug and play");
        }

        [TestMethod]
        public async Task DeviceTwin_Contains_ModelId_X509()
        {
            // Setup

            // Create a device.
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);
            // Send model ID with MQTT connect packet to make the device plug and play.
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = TestModelId,
            };
            string hostName = HostNameHelper.GetHostName(TestConfiguration.IotHub.ConnectionString);
            X509Certificate2 authCertificate = TestConfiguration.IotHub.GetCertificateWithPrivateKey();
            var auth = new ClientAuthenticationWithX509Certificate(authCertificate, testDevice.Id);
            await using var deviceClient = new IotHubDeviceClient(hostName, auth, options);
            await TestDevice.OpenWithRetryAsync(deviceClient).ConfigureAwait(false);

            // Act
            try
            {
                // Get device twin.
                using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
                ClientTwin twin = await serviceClient.Twins.GetAsync(testDevice.Device.Id).ConfigureAwait(false);

                // Assert
                twin.ModelId.Should().Be(TestModelId, "because the device was created as plug and play");
            }
            finally
            {
                // Cleanup
                if (authCertificate is IDisposable disposableCert)
                {
                    disposableCert?.Dispose();
                }
                authCertificate = null;
            }
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId()
        {
            // Setup

            // Create a module.
            await using TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);

            // Send model ID with MQTT connect packet to make the module plug and play.
            var options = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = TestModelId,
            };
            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get module twin.
            ClientTwin twin = await TestDevice.ServiceClient.Twins
                .GetAsync(testModule.DeviceId, testModule.Id)
                .ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the module was created as plug and play");
        }
    }
}
