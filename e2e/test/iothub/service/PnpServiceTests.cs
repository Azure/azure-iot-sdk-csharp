// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    /// <summary>
    /// Test class containing all tests to be run for plug and play.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
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
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            // Send model ID with MQTT connect packet to make the device plug and play.
            var options = new ClientOptions
            {
                ModelId = TestModelId,
            };
            using var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Mqtt_Tcp_Only, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get device twin.
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            Twin twin = await registryManager.GetTwinAsync(testDevice.Device.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the device was created as plug and play");

            // Cleanup
            await registryClient.RemoveDeviceAsync(testDevice.Id).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_Contains_ModelId_X509()
        {
            // Setup

            // Create a device.
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix, TestDeviceType.X509).ConfigureAwait(false);
            // Send model ID with MQTT connect packet to make the device plug and play.
            var options = new ClientOptions
            {
                ModelId = TestModelId,
            };
            string hostName = HostNameHelper.GetHostName(TestConfiguration.IoTHub.ConnectionString);
            X509Certificate2 authCertificate = TestConfiguration.IoTHub.GetCertificateWithPrivateKey();
            using var auth = new DeviceAuthenticationWithX509Certificate(testDevice.Id, authCertificate);
            using var deviceClient = DeviceClient.Create(hostName, auth, Client.TransportType.Mqtt_Tcp_Only, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get device twin.
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            Twin twin = await registryManager.GetTwinAsync(testDevice.Device.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the device was created as plug and play");

            // Cleanup
            await registryClient.RemoveDeviceAsync(testDevice.Id).ConfigureAwait(false);

            if (authCertificate is IDisposable disposableCert)
            {
                disposableCert?.Dispose();
            }
            authCertificate = null;
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId()
        {
            // Setup

            // Create a module.
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, Logger).ConfigureAwait(false);
            // Send model ID with MQTT connect packet to make the module plug and play.
            var options = new ClientOptions
            {
                ModelId = TestModelId,
            };
            using var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, Client.TransportType.Mqtt_Tcp_Only, options);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get module twin.
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            Twin twin = await registryManager.GetTwinAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the module was created as plug and play");

            // Cleanup
            await registryClient.RemoveDeviceAsync(testModule.DeviceId).ConfigureAwait(false);
        }
    }
}
