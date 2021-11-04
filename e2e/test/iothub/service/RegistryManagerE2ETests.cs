// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
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
        private readonly string _idPrefix = $"E2E_{nameof(RegistryManagerE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [ExpectedException(typeof(Common.Exceptions.IotHubCommunicationException))]
        public async Task RegistryManager_BadProxy_ThrowsException()
        {
            // arrange
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(
                TestConfiguration.IoTHub.ConnectionString,
                new HttpTransportSettings
                {
                    Proxy = new WebProxy(TestConfiguration.IoTHub.InvalidProxyServerAddress),
                });

            // act
            _ = await registryManager.GetDeviceAsync("device-that-does-not-exist").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddAndRemoveDeviceWithScope()
        {
            // arrange

            string edgeId1 = _idPrefix + Guid.NewGuid();
            string edgeId2 = _idPrefix + Guid.NewGuid();
            string deviceId = _idPrefix + Guid.NewGuid();

            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                // act

                // Create a top-level edge device.
                var edgeDevice1 = new Device(edgeId1)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true }
                };
                edgeDevice1 = await registryManager.AddDeviceAsync(edgeDevice1).ConfigureAwait(false);

                // Create a second-level edge device with edge 1 as the parent.
                var edgeDevice2 = new Device(edgeId2)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true },
                    ParentScopes = { edgeDevice1.Scope },
                };
                edgeDevice2 = await registryManager.AddDeviceAsync(edgeDevice2).ConfigureAwait(false);

                // Create a leaf device with edge 2 as the parent.
                var leafDevice = new Device(deviceId) { Scope = edgeDevice2.Scope };
                leafDevice = await registryManager.AddDeviceAsync(leafDevice).ConfigureAwait(false);

                // assert

                edgeDevice2.ParentScopes.FirstOrDefault().Should().Be(edgeDevice1.Scope, "The parent scope should be respected as set.");

                leafDevice.Id.Should().Be(deviceId, "The device Id should be respected as set.");
                leafDevice.Scope.Should().Be(edgeDevice2.Scope, "The device scope should be respected as set.");
                leafDevice.ParentScopes.FirstOrDefault().Should().Be(edgeDevice2.Scope, "The service should have copied the edge's scope to the leaf device's parent scope array.");
            }
            finally
            {
                // clean up

                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(edgeId1).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(edgeId2).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _idPrefix + Guid.NewGuid();

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
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

        [LoggedTestMethod]
        public async Task RegistryManager_BulkLifecycle()
        {
            int bulkCount = 50;
            var devices = new List<Device>();
            for (int i = 0; i < bulkCount; i++)
            {
                var device = new Device(_idPrefix + Guid.NewGuid());
                device.Scope = "someScope" + Guid.NewGuid();
                device.ParentScopes.Add("someParentScope" + Guid.NewGuid());
                devices.Add(device);
            }

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            // Test that you can create devices in bulk
            BulkRegistryOperationResult bulkAddResult = await registryManager.AddDevices2Async(devices).ConfigureAwait(false);
            Assert.IsTrue(bulkAddResult.IsSuccessful);

            foreach (Device device in devices)
            {
                // After a bulk add, every device should be able to be retrieved
                Device retrievedDevice = await registryManager.GetDeviceAsync(device.Id).ConfigureAwait(false);
                Assert.IsNotNull(retrievedDevice.Id);
                Assert.AreEqual(device.Scope, retrievedDevice.Scope);
                Assert.AreEqual(1, retrievedDevice.ParentScopes.Count);
                Assert.AreEqual(device.ParentScopes.ElementAt(0), retrievedDevice.ParentScopes.ElementAt(0));
            }

            var twins = new List<Twin>();
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
            BulkRegistryOperationResult bulkDeleteResult = await registryManager.RemoveDevices2Async(devices, true, default).ConfigureAwait(false);

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
            string deviceId = _idPrefix + Guid.NewGuid();
            var transportSettings = new HttpTransportSettings
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };

            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString, transportSettings);
            var device = new Device(deviceId);
            await registryManager.AddDeviceAsync(device).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_ConfigurationOperations_Work()
        {
            // arrange

            bool configCreated = false;
            string configurationId = (_idPrefix + Guid.NewGuid()).ToLower(); // Configuration Id characters must be all lower-case.
            using RegistryManager client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                var expected = new Configuration(configurationId)
                {
                    Priority = 2,
                    Labels = { { "labelName", "labelValue" } },
                    TargetCondition = "*",
                    Content =
                    {
                        DeviceContent = { { "properties.desired.x", 4L } },
                    },
                    Metrics =
                    {
                        Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 4" } }
                    },
                };

                // act and assert

                Configuration addResult = await client.AddConfigurationAsync(expected).ConfigureAwait(false);
                configCreated = true;
                addResult.Id.Should().Be(configurationId);
                addResult.Priority.Should().Be(expected.Priority);
                addResult.TargetCondition.Should().Be(expected.TargetCondition);
                addResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                addResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                addResult.ETag.Should().NotBeNullOrEmpty();

                Configuration getResult = await client.GetConfigurationAsync(configurationId).ConfigureAwait(false);
                getResult.Id.Should().Be(configurationId);
                getResult.Priority.Should().Be(expected.Priority);
                getResult.TargetCondition.Should().Be(expected.TargetCondition);
                getResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                getResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                getResult.ETag.Should().Be(addResult.ETag);

                IEnumerable<Configuration> listResult = await client.GetConfigurationsAsync(100).ConfigureAwait(false);
                listResult.Should().Contain(x => x.Id == configurationId);

                expected.Priority++;
                expected.ETag = getResult.ETag;
                Configuration updateResult = await client.UpdateConfigurationAsync(expected).ConfigureAwait(false);
                updateResult.Id.Should().Be(configurationId);
                updateResult.Priority.Should().Be(expected.Priority);
                updateResult.TargetCondition.Should().Be(expected.TargetCondition);
                updateResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                updateResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                updateResult.ETag.Should().NotBeNullOrEmpty().And.Should().NotBe(getResult.ETag, "The ETag should have changed after update");
            }
            finally
            {
                if (configCreated)
                {
                    // If this fails, we shall let it throw an exception and fail the test
                    await client.RemoveConfigurationAsync(configurationId).ConfigureAwait(false);
                }
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_Query_Works()
        {
            // arrange
            using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            string deviceId = $"{_idPrefix}{Guid.NewGuid()}";

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

                    if (twins.Any())
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
            using RegistryManager client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

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
                await CleanupAsync(client, testDeviceId).ConfigureAwait(false);
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

            using RegistryManager client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

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
                await CleanupAsync(client, testDeviceId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test basic operations of a module's twin.
        /// </summary>
        [LoggedTestMethod]
        public async Task ModulesClient_DeviceTwinLifecycle()
        {
            using RegistryManager client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            TestModule module = await TestModule.GetTestModuleAsync(_idPrefix, _idPrefix, Logger).ConfigureAwait(false);

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
                await CleanupAsync(client, module.DeviceId).ConfigureAwait(false);
            }
        }

        private async Task CleanupAsync(RegistryManager client, string deviceId)
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
