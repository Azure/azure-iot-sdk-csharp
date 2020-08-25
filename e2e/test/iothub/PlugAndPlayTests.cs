// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// Test class containing all tests to be run for plug and play.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("PlugAndPlay")]
    public class PlugAndPlayTests : E2EMsTestBase
    {
        private const string DevicePrefix = "plugAndPlayDevice";
        private const string ModulePrefix = "plugAndPlayModule";
        private const string TestModelId = "dtmi:com:example:testModel;1";

        [TestMethod]
        public async Task DeviceTwin_Sas_Contains_ModelId_ConnectOver_Mqtt_Tcp()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Mqtt_Tcp_Only, TestDeviceType.Sasl, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_Sas_Contains_ModelId_ConnectOver_Mqtt_Ws()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Mqtt_WebSocket_Only, TestDeviceType.Sasl, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_X509_Contains_ModelId_ConnectOver_Mqtt_Tcp()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Mqtt_Tcp_Only, TestDeviceType.X509, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_X509_Contains_ModelId_ConnectOver_Mqtt_Ws()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Mqtt_WebSocket_Only, TestDeviceType.Sasl, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_Sas_Contains_ModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, TestDeviceType.Sasl, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_Sas_Contains_ModelId_ConnectOver_Amqp_Ws()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, TestDeviceType.Sasl, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_X509_Contains_ModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, TestDeviceType.X509, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceTwin_X509_Contains_ModelId_ConnectOver_Amqp_Ws()
        {
            await CreateDeviceAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, TestDeviceType.X509, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId_ConnectOver_Mqtt_Tcp()
        {
            await CreateModuleAndRetrieveTwinAsync(Client.TransportType.Mqtt_Tcp_Only, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId_ConnectOver_Mqtt_Ws()
        {
            await CreateModuleAndRetrieveTwinAsync(Client.TransportType.Mqtt_WebSocket_Only, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateModuleAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleTwin_Contains_ModelId_ConnectOver_Amqp_Ws()
        {
            await CreateModuleAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedDevices_DeviceSak_HaveUniqueModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateMultiplexedDevicesAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.Device, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedDevices_DeviceSak_HaveUniqueModelId_ConnectOver_Amqp_Ws()
        {
            await CreateMultiplexedDevicesAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.Device, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedDevices_IotHubSak_HaveUniqueModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateMultiplexedDevicesAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.IoTHub, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedDevices_IotHubSak_HaveUniqueModelId_ConnectOver_Amqp_Ws()
        {
            await CreateMultiplexedDevicesAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.IoTHub, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedModules_HaveUniqueModelId_ConnectOver_Amqp_Tcp()
        {
            await CreateMultiplexedModulesAndRetrieveTwinAsync(Client.TransportType.Amqp_Tcp_Only, Logger).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiplexedModules_HaveUniqueModelId_ConnectOver_Amqp_Ws()
        {
            await CreateMultiplexedModulesAndRetrieveTwinAsync(Client.TransportType.Amqp_WebSocket_Only, Logger).ConfigureAwait(false);
        }

        private static async Task CreateMultiplexedDevicesAndRetrieveTwinAsync(Client.TransportType transportType, ConnectionStringAuthScope authScope, MsTestLogger logger)
        {
            // Setup
            var devices = new Dictionary<TestDevice, DeviceClient>();
            var operations = new List<Task>();

            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transportType)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)PoolingOverAmqp.MultipleConnections_PoolSize),
                        Pooling = true
                    }
                }
            };

            // With PoolingOverAmqp.MultipleConnections_DevicesCount = 4, and PoolingOverAmqp.MultipleConnections_PoolSize = 2:
            // Create a total of 4 device client instances, multiplexed over 2 amqp connections. Each device client instance uses a different ModelId.
            string modelIdPrefix = "dtmi:com:example:testModel";
            for (int i = 0; i < PoolingOverAmqp.MultipleConnections_DevicesCount; i++)
            {
                TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, DevicePrefix, TestDeviceType.Sasl).ConfigureAwait(false);

                string modelId = $"{modelIdPrefix};{testDevice.Id}";
                DeviceClient deviceClient = testDevice.CreateDeviceClient(transportSettings, authScope, new ClientOptions { ModelId = modelId });
                devices.Add(testDevice, deviceClient);
            }

            // Calling OpenAsync() will cause the client to open the connection.
            foreach (DeviceClient client in devices.Values)
            {
                operations.Add(client.OpenAsync());
            }
            await Task.WhenAll(operations).ConfigureAwait(false);

            // Act and Assert

            // Retrieve the device twin and assert the value of ModelId returned.
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            foreach (TestDevice device in devices.Keys)
            {
                string deviceId = device.Id;
                Twin twin = await registryManager.GetTwinAsync(deviceId).ConfigureAwait(false);
                twin.ModelId.Should().Be($"{modelIdPrefix};{deviceId}", "because the device was created as plug and play");
            }

            // Cleanup
            foreach (TestDevice device in devices.Keys)
            {
                await registryManager.RemoveDeviceAsync(device.Id).ConfigureAwait(false);
            }
        }

        private static async Task CreateMultiplexedModulesAndRetrieveTwinAsync(Client.TransportType transportType, MsTestLogger logger)
        {
            // Setup
            var modules = new Dictionary<TestModule, ModuleClient>();
            var operations = new List<Task>();

            var transportSettings = new ITransportSettings[]
            {
                new AmqpTransportSettings(transportType)
                {
                    AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                    {
                        MaxPoolSize = unchecked((uint)PoolingOverAmqp.MultipleConnections_PoolSize),
                        Pooling = true
                    }
                }
            };

            // With PoolingOverAmqp.MultipleConnections_DevicesCount = 4, and PoolingOverAmqp.MultipleConnections_PoolSize = 2:
            // Create a total of 4 module client instances, multiplexed over 2 amqp connections. Each module client instance uses a different ModelId.
            string modelIdPrefix = "dtmi:com:example:testModel";
            for (int i = 0; i < PoolingOverAmqp.MultipleConnections_DevicesCount; i++)
            {
                TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, logger).ConfigureAwait(false);

                string modelId = $"{modelIdPrefix};{testModule.Id}";
                modules.Add(testModule, CreateModuleClientWithModelId(testModule, transportSettings, modelId, logger));
            }

            // Calling OpenAsync() will cause the client to open the connection.
            foreach (ModuleClient client in modules.Values)
            {
                operations.Add(client.OpenAsync());
            }
            await Task.WhenAll(operations).ConfigureAwait(false);

            // Act and Assert

            // Retrieve the module twin and assert the value of ModelId returned.
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            foreach (TestModule module in modules.Keys)
            {
                Twin twin = await registryManager.GetTwinAsync(module.DeviceId, module.Id).ConfigureAwait(false);
                twin.ModelId.Should().Be($"{modelIdPrefix};{module.Id}", "because the module was created as plug and play");
            }

            // Cleanup
            foreach (TestModule module in modules.Keys)
            {
                await registryManager.RemoveDeviceAsync(module.DeviceId).ConfigureAwait(false);
            }
        }

        private static ModuleClient CreateModuleClientWithModelId(TestModule testModule, ITransportSettings[] transportSettings, string modelId, MsTestLogger logger)
        {
            // Send model ID while initializing the module client, to mark the module as plug and play compatible.
            var options = new ClientOptions
            {
                ModelId = modelId,
            };
            var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings, options);
            logger.Trace($"{nameof(PlugAndPlayTests)}: Created module: deviceId={testModule.DeviceId}, moduleId={testModule.Id}, modelId={modelId}");

            return moduleClient;
        }

        private static ModuleClient CreateModuleClientWithModelId(TestModule testModule, Client.TransportType transportType, string modelId, MsTestLogger logger)
        {
            // Send model ID while initializing the module client, to mark the module as plug and play compatible.
            var options = new ClientOptions
            {
                ModelId = modelId,
            };
            var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportType, options);
            logger.Trace($"{nameof(PlugAndPlayTests)}: Created module: deviceId={testModule.DeviceId}, moduleId={testModule.Id}, modelId={modelId}, transportType={transportType}");

            return moduleClient;
        }

        private static async Task CreateDeviceAndRetrieveTwinAsync(Client.TransportType transportType, TestDeviceType deviceType, MsTestLogger logger)
        {
            // Setup

            // Create a device.
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, DevicePrefix, deviceType).ConfigureAwait(false);

            // Send model ID while initializing the device client, to mark the device as plug and play compatible.
            var options = new ClientOptions
            {
                ModelId = TestModelId,
            };
            using DeviceClient deviceClient = testDevice.CreateDeviceClient(transportType, options);
            logger.Trace($"{nameof(PlugAndPlayTests)}: Created device: deviceId={testDevice.Id}, modelId={options.ModelId}, auth={testDevice.Device.Authentication}, transport={transportType}");

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get device twin.
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            Twin twin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the device was created as plug and play");

            // Cleanup
            await registryManager.RemoveDeviceAsync(testDevice.Id).ConfigureAwait(false);
        }

        private static async Task CreateModuleAndRetrieveTwinAsync(Client.TransportType transportType, MsTestLogger logger)
        {
            // Setup

            // Create a module.
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, logger).ConfigureAwait(false);

            using ModuleClient moduleClient = CreateModuleClientWithModelId(testModule, transportType, TestModelId, logger);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Act

            // Get module twin.
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            Twin twin = await registryManager.GetTwinAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            // Assert
            twin.ModelId.Should().Be(TestModelId, "because the module was created as plug and play");

            // Cleanup
            await registryManager.RemoveDeviceAsync(testModule.DeviceId).ConfigureAwait(false);
        }
    }
}
