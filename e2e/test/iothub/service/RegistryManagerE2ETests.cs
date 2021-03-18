// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryManagerE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(RegistryManagerE2ETests)}_";
        private readonly string _modulePrefix = $"E2E_{nameof(RegistryManagerE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [ExpectedException(typeof(Common.Exceptions.IotHubCommunicationException))]
        public async Task RegistryManager_BadProxy_ThrowsException()
        {
            // arrange
            using var registryManager = RegistryManager.CreateFromConnectionString(
                Configuration.IoTHub.ConnectionString,
                new HttpTransportSettings
                {
                    Proxy = new WebProxy(Configuration.IoTHub.InvalidProxyServerAddress),
                });

            // act
            _ = await registryManager.GetDeviceAsync("device-that-does-not-exist").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddAndRemoveDeviceWithScope()
        {
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            string deviceId = _devicePrefix + Guid.NewGuid();

            var edgeDevice = new Device(deviceId)
            {
                Capabilities = new DeviceCapabilities { IotEdge = true }
            };
            edgeDevice = await registryManager.AddDeviceAsync(edgeDevice).ConfigureAwait(false);

            var leafDevice = new Device(Guid.NewGuid().ToString()) { Scope = edgeDevice.Scope };
            Device receivedDevice = await registryManager.AddDeviceAsync(leafDevice).ConfigureAwait(false);

            Assert.IsNotNull(receivedDevice);
            Assert.AreEqual(leafDevice.Id, receivedDevice.Id, $"Expected Device ID={leafDevice.Id}; Actual Device ID={receivedDevice.Id}");
            Assert.AreEqual(leafDevice.Scope, receivedDevice.Scope, $"Expected Device Scope={leafDevice.Scope}; Actual Device Scope={receivedDevice.Scope}");
            await registryManager.RemoveDeviceAsync(leafDevice.Id).ConfigureAwait(false);
            await registryManager.RemoveDeviceAsync(edgeDevice.Id).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _devicePrefix + Guid.NewGuid();

            using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twin = new Twin
                {
                    Tags = new TwinCollection(@"{ companyId: 1234 }"),
                };

                var iotEdgeDevice = new Device(deviceId)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true }
                };

                await registryManager.AddDeviceWithTwinAsync(iotEdgeDevice, twin).ConfigureAwait(false);

                Device actual = await registryManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);

                Assert.IsTrue(actual.Capabilities.IotEdge);
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_BulkLifecycle()
        {
            int bulkCount = 50;
            List<Device> devices = new List<Device>();
            for (int i = 0; i < bulkCount; i++)
            {
                devices.Add(new Device(_devicePrefix + Guid.NewGuid()));
            }

            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            // Test that you can create devices in bulk
            var bulkAddResult = await registryManager.AddDevices2Async(devices).ConfigureAwait(false);
            Assert.IsTrue(bulkAddResult.IsSuccessful);

            foreach (Device device in devices)
            {
                // After a bulk add, every device should be able to be retrieved
                Assert.IsNotNull(await registryManager.GetDeviceAsync(device.Id).ConfigureAwait(false));
            }

            List<Twin> twins = new List<Twin>();
            string expectedProperty = "someNewProperty";
            string expectedPropertyValue = "someNewPropertyValue";
            foreach (Device device in devices)
            {
                Twin twin = await registryManager.GetTwinAsync(device.Id).ConfigureAwait(false);
                twin.Properties.Desired[expectedProperty] = expectedPropertyValue;
                twins.Add(twin);
            }

            // Test that you can update twins in bulk
            await registryManager.UpdateTwins2Async(twins).ConfigureAwait(false);

            foreach (Device device in devices)
            {
                Twin twin = await registryManager.GetTwinAsync(device.Id).ConfigureAwait(false);
                Assert.IsNotNull(twin.Properties.Desired[expectedProperty]);
                Assert.AreEqual(expectedPropertyValue, (string)twin.Properties.Desired[expectedProperty]);
            }

            // Test that you can delete device identities in bulk
            var bulkDeleteResult = await registryManager.RemoveDevices2Async(devices, true, default).ConfigureAwait(false);

            Assert.IsTrue(bulkDeleteResult.IsSuccessful);

            foreach (Device device in devices)
            {
                // After a bulk delete, every device should not be found
                Assert.IsNull(await registryManager.GetDeviceAsync(device.Id).ConfigureAwait(false));
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddDeviceWithProxy()
        {
            string deviceId = _devicePrefix + Guid.NewGuid();
            var transportSettings = new HttpTransportSettings
            {
                Proxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, transportSettings))
            {
                var device = new Device(deviceId);
                await registryManager.AddDeviceAsync(device).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_Query_Works()
        {
            // arrange
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            string deviceId = $"{_devicePrefix}{Guid.NewGuid()}";

            try
            {
                Device device = await registryManager
                    .AddDeviceAsync(new Device(deviceId))
                    .ConfigureAwait(false);

                // act

                IQuery query = null;
                IEnumerable<Twin> twins = null;
                for (int i = 0; i < 30; ++i)
                {
                    string queryText = $"select * from devices where deviceId = '{deviceId}'";
                    query = registryManager.CreateQuery(queryText);

                    twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                    if (twins.Count() > 0)
                    {
                        break;
                    }

                    // A new device may not return immediately from a query, so give it some time and some retries to appear
                    await Task.Delay(250).ConfigureAwait(false);
                }

                // assert
                twins.Count().Should().Be(1, "only asked for 1 device by its Id");
                twins.First().DeviceId.Should().Be(deviceId, "The Id of the device returned should match");
                query.HasMoreResults.Should().BeFalse("We've processed the single, expected result");
            }
            finally
            {
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task ModulesClient_GetModulesOnDevice()
        {
            int moduleCount = 5;
            string testDeviceId = $"IdentityLifecycleDevice{Guid.NewGuid()}";
            string[] testModuleIds = new string[moduleCount];
            for (int i = 0; i < moduleCount; i++)
            {
                testModuleIds[i] = $"IdentityLifecycleModule{i}-{Guid.NewGuid()}";
            }

            Device device = null;
            using RegistryManager client = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            try
            {
                // Create a device to house the modules
                device = await client.AddDeviceAsync(new Device(testDeviceId)).ConfigureAwait(false);

                // Create the modules on the device
                for (int i = 0; i < moduleCount; i++)
                {
                    Module createdModule = await client.AddModuleAsync(
                        new Module(testDeviceId, testModuleIds[i])).ConfigureAwait(false);
                }

                // List the modules on the test device
                IEnumerable<Module> modulesOnDevice = await client.GetModulesOnDeviceAsync(testDeviceId).ConfigureAwait(false);

                IList<string> moduleIdsOnDevice = modulesOnDevice
                    .Select(module => module.Id)
                    .ToList();

                Assert.AreEqual(moduleCount, moduleIdsOnDevice.Count);
                for (int i = 0; i < moduleCount; i++)
                {
                    Assert.IsTrue(moduleIdsOnDevice.Contains(testModuleIds[i]));
                }
            }
            finally
            {
                await Cleanup(client, testDeviceId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test basic lifecycle of a module.
        /// This test includes CRUD operations only.
        /// </summary>
        [LoggedTestMethod]
        public async Task ModulesClient_IdentityLifecycle()
        {
            string testDeviceId = $"IdentityLifecycleDevice{Guid.NewGuid()}";
            string testModuleId = $"IdentityLifecycleModule{Guid.NewGuid()}";

            using RegistryManager client = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            try
            {
                // Create a device to house the module
                Device device = await client.AddDeviceAsync(new Device(testDeviceId)).ConfigureAwait(false);

                // Create a module on the device
                Module createdModule = await client.AddModuleAsync(
                    new Module(testDeviceId, testModuleId)).ConfigureAwait(false);

                createdModule.DeviceId.Should().Be(testDeviceId);
                createdModule.Id.Should().Be(testModuleId);

                // Get device
                // Get the device and compare ETag values (should remain unchanged);
                Module retrievedModule = await client.GetModuleAsync(testDeviceId, testModuleId).ConfigureAwait(false);

                retrievedModule.ETag.Should().BeEquivalentTo(createdModule.ETag, "ETag value should not have changed between create and get.");

                // Update a module
                string managedByValue = "SomeChangedValue";
                retrievedModule.ManagedBy = managedByValue;

                Module updatedModule = await client.UpdateModuleAsync(retrievedModule).ConfigureAwait(false);

                updatedModule.ManagedBy.Should().Be(managedByValue, "Module should have changed its managedBy value");

                // Delete the device
                // Deleting the device happens in the finally block as cleanup.
            }
            finally
            {
                await Cleanup(client, testDeviceId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test basic operations of a module's twin.
        /// </summary>
        [LoggedTestMethod]
        public async Task ModulesClient_DeviceTwinLifecycle()
        {
            using RegistryManager client = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            TestModule module = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);

            try
            {
                // Get the module twin
                Twin moduleTwin = await client.GetTwinAsync(module.DeviceId, module.Id).ConfigureAwait(false);

                moduleTwin.ModuleId.Should().BeEquivalentTo(module.Id, "ModuleId on the Twin should match that of the module identity.");

                // Update device twin
                string propName = "username";
                string propValue = "userA";
                moduleTwin.Properties.Desired[propName] = propValue;

                Twin updatedModuleTwin = await client.UpdateTwinAsync(module.DeviceId, module.Id, moduleTwin, moduleTwin.ETag).ConfigureAwait(false);

                Assert.IsNotNull(updatedModuleTwin.Properties.Desired[propName]);
                Assert.AreEqual(propValue, (string)updatedModuleTwin.Properties.Desired[propName]);

                // Delete the module
                // Deleting the module happens in the finally block as cleanup.
            }
            finally
            {
                await Cleanup(client, module.DeviceId).ConfigureAwait(false);
            }
        }

        private async Task Cleanup(RegistryManager client, string deviceId)
        {
            // cleanup
            try
            {
                if (deviceId != null)
                {
                    await client.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test clean up failed: {ex.Message}");
            }
        }
    }
}
