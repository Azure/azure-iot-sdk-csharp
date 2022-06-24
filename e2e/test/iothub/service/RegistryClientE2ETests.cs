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
using Microsoft.Azure.Devices.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"E2E_{nameof(RegistryClientE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [ExpectedException(typeof(Common.Exceptions.IotHubCommunicationException))]
        public async Task registryClient_BadProxy_ThrowsException()
        {
            // arrange
            using var registryClient = new RegistryClient(
                TestConfiguration.IoTHub.ConnectionString,
                new HttpTransportSettings2
                {
                    Proxy = new WebProxy(TestConfiguration.IoTHub.InvalidProxyServerAddress),
                });

            // act
            _ = await registryClient.GetDeviceAsync("device-that-does-not-exist").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task registryClient_AddAndRemoveDeviceWithScope()
        {
            // arrange

            string edgeId1 = _idPrefix + Guid.NewGuid();
            string edgeId2 = _idPrefix + Guid.NewGuid();
            string deviceId = _idPrefix + Guid.NewGuid();

            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                // act

                // Create a top-level edge device.
                var edgeDevice1 = new Device(edgeId1)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true }
                };
                edgeDevice1 = await registryClient.AddDeviceAsync(edgeDevice1).ConfigureAwait(false);

                // Create a second-level edge device with edge 1 as the parent.
                var edgeDevice2 = new Device(edgeId2)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true },
                    ParentScopes = { edgeDevice1.Scope },
                };
                edgeDevice2 = await registryClient.AddDeviceAsync(edgeDevice2).ConfigureAwait(false);

                // Create a leaf device with edge 2 as the parent.
                var leafDevice = new Device(deviceId) { Scope = edgeDevice2.Scope };
                leafDevice = await registryClient.AddDeviceAsync(leafDevice).ConfigureAwait(false);

                // assert

                edgeDevice2.ParentScopes.FirstOrDefault().Should().Be(edgeDevice1.Scope, "The parent scope should be respected as set.");

                leafDevice.Id.Should().Be(deviceId, "The device Id should be respected as set.");
                leafDevice.Scope.Should().Be(edgeDevice2.Scope, "The device scope should be respected as set.");
                leafDevice.ParentScopes.FirstOrDefault().Should().Be(edgeDevice2.Scope, "The service should have copied the edge's scope to the leaf device's parent scope array.");
            }
            finally
            {
                // clean up

                await registryClient.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                await registryClient.RemoveDeviceAsync(edgeId1).ConfigureAwait(false);
                await registryClient.RemoveDeviceAsync(edgeId2).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task registryClient_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _idPrefix + Guid.NewGuid();

            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            var twin = new Twin
            {
                Tags = new TwinCollection(@"{ companyId: 1234 }"),
            };

            var iotEdgeDevice = new Device(deviceId)
            {
                Capabilities = new DeviceCapabilities { IotEdge = true }
            };

            await registryClient.AddDeviceWithTwinAsync(iotEdgeDevice, twin).ConfigureAwait(false);

            Device actual = await registryClient.GetDeviceAsync(deviceId).ConfigureAwait(false);
            await registryClient.RemoveDeviceAsync(deviceId).ConfigureAwait(false);

            Assert.IsTrue(actual.Capabilities.IotEdge);
        }

        [LoggedTestMethod]
        public async Task registryClient_AddDevices2Async_Works()
        {
            // arrange

            var edge = new Device(_idPrefix + Guid.NewGuid())
            {
                Scope = "someScope" + Guid.NewGuid(),
            };
            var device = new Device(_idPrefix + Guid.NewGuid())
            {
                Scope = edge.Scope,
            };

            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                // act
                BulkRegistryOperationResult bulkAddResult = await registryClient
                    .AddDevicesAsync(new List<Device> { edge, device })
                    .ConfigureAwait(false);

                // assert

                bulkAddResult.IsSuccessful.Should().BeTrue();

                Device actualEdge = await registryClient.GetDeviceAsync(edge.Id).ConfigureAwait(false);
                actualEdge.Id.Should().Be(edge.Id);
                actualEdge.Scope.Should().Be(edge.Scope);

                Device actualDevice = await registryClient.GetDeviceAsync(device.Id).ConfigureAwait(false);
                actualDevice.Id.Should().Be(device.Id);
                actualDevice.Scope.Should().Be(device.Scope);
                actualDevice.ParentScopes.Count.Should().Be(1);
                actualDevice.ParentScopes.First().Should().Be(edge.Scope);
            }
            finally
            {
                try
                {
                    await registryClient.RemoveDeviceAsync(device.Id).ConfigureAwait(false);
                    await registryClient.RemoveDeviceAsync(edge.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod]
        public async Task registryClient_UpdateDevices2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            var edge = new Device(_idPrefix + Guid.NewGuid());
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                Device addedDevice1 = await registryClient.AddDeviceAsync(device1).ConfigureAwait(false);
                Device addedDevice2 = await registryClient.AddDeviceAsync(device2).ConfigureAwait(false);
                Device addedEdge = await registryClient.AddDeviceAsync(edge).ConfigureAwait(false);

                // act

                addedDevice1.Scope = addedEdge.Scope;
                addedDevice2.Scope = addedEdge.Scope;
                BulkRegistryOperationResult result = await registryClient
                    .UpdateDevicesAsync(new[] { addedDevice1, addedDevice2 })
                    .ConfigureAwait(false);

                // assert

                result.IsSuccessful.Should().BeTrue();

                Device actualDevice1 = await registryClient.GetDeviceAsync(device1.Id).ConfigureAwait(false);
                actualDevice1.Scope.Should().Be(addedEdge.Scope);

                Device actualDevice2 = await registryClient.GetDeviceAsync(device2.Id).ConfigureAwait(false);
                actualDevice2.Scope.Should().Be(addedEdge.Scope);
            }
            finally
            {
                try
                {
                    await registryClient.RemoveDeviceAsync(device1.Id).ConfigureAwait(false);
                    await registryClient.RemoveDeviceAsync(device2.Id).ConfigureAwait(false);
                    await registryClient.RemoveDeviceAsync(edge.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod]
        public async Task registryClient_UpdateTwins2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                await registryClient.AddDeviceAsync(device1).ConfigureAwait(false);
                Twin twin1 = await registryManager.GetTwinAsync(device1.Id).ConfigureAwait(false);
                await registryClient.AddDeviceAsync(device2).ConfigureAwait(false);
                Twin twin2 = await registryManager.GetTwinAsync(device2.Id).ConfigureAwait(false);

                // act

                const string expectedProperty = "someNewProperty";
                const string expectedPropertyValue = "someNewPropertyValue";

                twin1.Properties.Desired[expectedProperty] = expectedPropertyValue;
                twin2.Properties.Desired[expectedProperty] = expectedPropertyValue;

                BulkRegistryOperationResult result = await registryManager
                    .UpdateTwinsAsync(new[] { twin1, twin2 })
                    .ConfigureAwait(false);

                // assert

                result.IsSuccessful.Should().BeTrue();

                var actualTwin1 = await registryManager.GetTwinAsync(device1.Id).ConfigureAwait(false);
                ((string)actualTwin1.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
                var actualTwin2 = await registryManager.GetTwinAsync(device2.Id).ConfigureAwait(false);
                ((string)(actualTwin2.Properties.Desired[expectedProperty])).Should().Be(expectedPropertyValue);
            }
            finally
            {
                try
                {
                    await registryClient.RemoveDeviceAsync(device1.Id).ConfigureAwait(false);
                    await registryClient.RemoveDeviceAsync(device2.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod]
        public async Task registryClient_RemoveDevices2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                await registryClient.AddDeviceAsync(device1).ConfigureAwait(false);
                await registryClient.AddDeviceAsync(device2).ConfigureAwait(false);

                // act

                BulkRegistryOperationResult bulkDeleteResult = await registryClient
                    .RemoveDevicesAsync(new[] { device1, device2 }, true, default)
                    .ConfigureAwait(false);

                // assert

                bulkDeleteResult.IsSuccessful.Should().BeTrue();

                try
                {
                    Device actualDevice1 = await registryClient.GetDeviceAsync(device1.Id).ConfigureAwait(false);
                    throw new AssertFailedException("Expected the request to fail with a \"not found\" error");
                }
                catch (DeviceNotFoundException)
                {
                    // expected exception
                }

                try
                {
                    Device actualDevice2 = await registryClient.GetDeviceAsync(device1.Id).ConfigureAwait(false);
                    throw new AssertFailedException("Expected the request to fail with a \"not found\" error");
                }
                catch (DeviceNotFoundException)
                {
                    // expected exception
                }
            }
            finally
            {
                try
                {
                    await registryClient.RemoveDeviceAsync(device1.Id).ConfigureAwait(false);
                    await registryClient.RemoveDeviceAsync(device2.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod]
        public async Task registryClient_AddDeviceWithProxy()
        {
            string deviceId = _idPrefix + Guid.NewGuid();
            var transportSettings = new HttpTransportSettings2
            {
                Proxy = new WebProxy(TestConfiguration.IoTHub.ProxyServerAddress)
            };

            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString, transportSettings);
            var device = new Device(deviceId);
            await registryClient.AddDeviceAsync(device).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task registryClient_ConfigurationOperations_Work()
        {
            // arrange

            bool configCreated = false;
            string configurationId = (_idPrefix + Guid.NewGuid()).ToLower(); // Configuration Id characters must be all lower-case.
            using var client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                var expected = new Configuration(configurationId)
                {
                    Priority = 2,
                    Labels = { { "labelName", "labelValue" } },
                    TargetCondition = "*",
                    Content = new ConfigurationContent
                    {
                        DeviceContent = { { "properties.desired.x", 4L } },
                    },
                    Metrics = new ConfigurationMetrics
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
        public async Task registryClient_Query_Works()
        {
            // arrange

            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            string deviceId = TestConfiguration.IoTHub.X509ChainDeviceName;

            Device device = await registryClient
                .GetDeviceAsync(deviceId)
                .ConfigureAwait(false);
            device.Should().NotBeNull($"Device {deviceId} should already exist in hub setup for E2E tests");

            // act

            string queryText = $"select * from devices where deviceId = '{deviceId}'";
            IQuery query = registryManager.CreateQuery(queryText);
            IEnumerable<Twin> twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

            // assert

            twins.Count().Should().Be(1, "only asked for 1 device by its Id");
            twins.First().DeviceId.Should().Be(deviceId, "The Id of the device returned should match");
            query.HasMoreResults.Should().BeFalse("We've processed the single, expected result");
        }

        [LoggedTestMethod]
        public async Task ModulesClient_GetModulesOnDevice()
        {
            const int moduleCount = 2;
            string testDeviceId = $"IdentityLifecycleDevice{Guid.NewGuid()}";
            string[] testModuleIds = new string[moduleCount];
            for (int i = 0; i < moduleCount; i++)
            {
                testModuleIds[i] = $"IdentityLifecycleModule{i}-{Guid.NewGuid()}";
            }

            Device device = null;
            using var client = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

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

                // Give the hub a moment
                await Task.Delay(250).ConfigureAwait(false);

                // List the modules on the test device
                IEnumerable<Module> modulesOnDevice = await client.GetModulesOnDeviceAsync(testDeviceId).ConfigureAwait(false);

                IList<string> moduleIdsOnDevice = modulesOnDevice
                    .Select(module => module.Id)
                    .ToList();

                Assert.AreEqual(moduleCount, moduleIdsOnDevice.Count);
                for (int i = 0; i < moduleCount; i++)
                {
                    moduleIdsOnDevice.Should().Contain(testModuleIds[i]);
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

            using var client = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);

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
            using var client = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var registryClient = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
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
                await CleanupAsync(registryClient, module.DeviceId).ConfigureAwait(false);
            }
        }

        private static async Task CleanupAsync(RegistryClient client, string deviceId)
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
