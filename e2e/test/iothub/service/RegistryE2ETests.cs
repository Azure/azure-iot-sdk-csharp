// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for all registry operations including device/module CRUD.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(RegistryE2ETests)}_";

        private static readonly IRetryPolicy s_provisioningServiceRetryPolicy = new ProvisioningServiceRetryPolicy();
        // In particular, this should retry on "module not registered on this device" errors
        private static readonly HashSet<Type> s_retryableExceptions = new HashSet<Type> { typeof(IotHubServiceException) };

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task DevicesClient_BadProxy_ThrowsException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                TestConfiguration.IotHub.ConnectionString,
                new IotHubServiceClientOptions
                {
                    Proxy = new WebProxy(TestConfiguration.IotHub.InvalidProxyServerAddress),
                });

            // act
            _ = await serviceClient.Devices.GetAsync("device-that-does-not-exist").ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddAndRemoveDeviceWithScope()
        {
            // arrange

            string edgeId1 = _idPrefix + Guid.NewGuid();
            string edgeId2 = _idPrefix + Guid.NewGuid();
            string deviceId = _idPrefix + Guid.NewGuid();

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                // act

                // Create a top-level edge device.
                var edgeDevice1 = new Device(edgeId1)
                {
                    Capabilities = new DeviceCapabilities { IsIotEdge = true }
                };
                edgeDevice1 = await serviceClient.Devices.CreateAsync(edgeDevice1).ConfigureAwait(false);

                // Create a second-level edge device with edge 1 as the parent.
                var edgeDevice2 = new Device(edgeId2)
                {
                    Capabilities = new DeviceCapabilities { IsIotEdge = true },
                    ParentScopes = { edgeDevice1.Scope },
                };
                edgeDevice2 = await serviceClient.Devices.CreateAsync(edgeDevice2).ConfigureAwait(false);

                // Create a leaf device with edge 2 as the parent.
                var leafDevice = new Device(deviceId) { Scope = edgeDevice2.Scope };
                leafDevice = await serviceClient.Devices.CreateAsync(leafDevice).ConfigureAwait(false);

                // assert

                edgeDevice2.ParentScopes.FirstOrDefault().Should().Be(edgeDevice1.Scope, "The parent scope should be respected as set.");

                leafDevice.Id.Should().Be(deviceId, "The device Id should be respected as set.");
                leafDevice.Scope.Should().Be(edgeDevice2.Scope, "The device scope should be respected as set.");
                leafDevice.ParentScopes.FirstOrDefault().Should().Be(edgeDevice2.Scope, "The service should have copied the edge's scope to the leaf device's parent scope array.");
            }
            finally
            {
                // clean up

                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
                await serviceClient.Devices.DeleteAsync(edgeId1).ConfigureAwait(false);
                await serviceClient.Devices.DeleteAsync(edgeId2).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _idPrefix + Guid.NewGuid();

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var twin = new Twin(deviceId)
            {
                Tags = new TwinCollection(@"{ companyId: 1234 }"),
            };

            var iotEdgeDevice = new Device(deviceId)
            {
                Capabilities = new DeviceCapabilities { IsIotEdge = true }
            };

            await serviceClient.Devices.CreateWithTwinAsync(iotEdgeDevice, twin).ConfigureAwait(false);

            try
            {
                Device actual = await serviceClient.Devices.GetAsync(deviceId).ConfigureAwait(false);
                actual.Should().NotBeNull($"Got null in GET on device {deviceId} to check IotEdge property.");
                actual.Capabilities.IsIotEdge.Should().BeTrue();
            }
            finally
            {
                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddDevices2Async_Works()
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

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                // act
                BulkRegistryOperationResult bulkAddResult =
                    await serviceClient.Devices.CreateAsync(new List<Device> { edge, device }).ConfigureAwait(false);

                // assert

                bulkAddResult.IsSuccessful.Should().BeTrue();

                Device actualEdge = await serviceClient.Devices.GetAsync(edge.Id).ConfigureAwait(false);
                actualEdge.Id.Should().Be(edge.Id);
                actualEdge.Scope.Should().Be(edge.Scope);

                Device actualDevice = await serviceClient.Devices.GetAsync(device.Id).ConfigureAwait(false);
                actualDevice.Id.Should().Be(device.Id);
                actualDevice.Scope.Should().Be(device.Scope);
                actualDevice.ParentScopes.Count.Should().Be(1);
                actualDevice.ParentScopes.First().Should().Be(edge.Scope);
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device.Id).ConfigureAwait(false);
                    await serviceClient.Devices.DeleteAsync(edge.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_UpdateDevices2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            var edge = new Device(_idPrefix + Guid.NewGuid());
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                Device addedDevice1 = await serviceClient.Devices.CreateAsync(device1).ConfigureAwait(false);
                Device addedDevice2 = await serviceClient.Devices.CreateAsync(device2).ConfigureAwait(false);
                Device addedEdge = await serviceClient.Devices.CreateAsync(edge).ConfigureAwait(false);

                // act

                addedDevice1.Scope = addedEdge.Scope;
                addedDevice2.Scope = addedEdge.Scope;
                BulkRegistryOperationResult result = await serviceClient.Devices
                    .SetAsync(new[] { addedDevice1, addedDevice2 })
                    .ConfigureAwait(false);

                // assert

                result.IsSuccessful.Should().BeTrue();

                Device actualDevice1 = await serviceClient.Devices.GetAsync(device1.Id).ConfigureAwait(false);
                actualDevice1.Scope.Should().Be(addedEdge.Scope);

                Device actualDevice2 = await serviceClient.Devices.GetAsync(device2.Id).ConfigureAwait(false);
                actualDevice2.Scope.Should().Be(addedEdge.Scope);
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device1.Id).ConfigureAwait(false);
                    await serviceClient.Devices.DeleteAsync(device2.Id).ConfigureAwait(false);
                    await serviceClient.Devices.DeleteAsync(edge.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task RegistryManager_UpdateTwins2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                await serviceClient.Devices.CreateAsync(device1).ConfigureAwait(false);
                Twin twin1 = await serviceClient.Twins.GetAsync(device1.Id).ConfigureAwait(false);
                await serviceClient.Devices.CreateAsync(device2).ConfigureAwait(false);
                Twin twin2 = await serviceClient.Twins.GetAsync(device2.Id).ConfigureAwait(false);

                // act

                const string expectedProperty = "someNewProperty";
                const string expectedPropertyValue = "someNewPropertyValue";

                twin1.Properties.Desired[expectedProperty] = expectedPropertyValue;
                twin2.Properties.Desired[expectedProperty] = expectedPropertyValue;

                BulkRegistryOperationResult result = await serviceClient.Twins
                    .UpdateAsync(new[] { twin1, twin2 }, false)
                    .ConfigureAwait(false);

                // assert

                result.IsSuccessful.Should().BeTrue();

                var actualTwin1 = await serviceClient.Twins.GetAsync(device1.Id).ConfigureAwait(false);
                ((string)actualTwin1.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
                var actualTwin2 = await serviceClient.Twins.GetAsync(device2.Id).ConfigureAwait(false);
                ((string)(actualTwin2.Properties.Desired[expectedProperty])).Should().Be(expectedPropertyValue);
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device1.Id).ConfigureAwait(false);
                    await serviceClient.Devices.DeleteAsync(device2.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_RemoveDevices2Async_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                await serviceClient.Devices.CreateAsync(device1).ConfigureAwait(false);
                await serviceClient.Devices.CreateAsync(device2).ConfigureAwait(false);

                // act
                BulkRegistryOperationResult bulkDeleteResult = await serviceClient.Devices
                    .DeleteAsync(new[] { device1, device2 }, false, default)
                    .ConfigureAwait(false);

                // assert
                bulkDeleteResult.IsSuccessful.Should().BeTrue();

                Func<Task> act1 = async () => await serviceClient.Devices.GetAsync(device1.Id).ConfigureAwait(false);

                var error1 = await act1.Should().ThrowAsync<IotHubServiceException>("Expected the request to fail with a \"not found\" error");
                error1.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
                error1.And.ErrorCode.Should().Be(IotHubErrorCode.DeviceNotFound);

                Func<Task> act2 = async () => await serviceClient.Devices.GetAsync(device1.Id).ConfigureAwait(false);

                var error2 = await act2.Should().ThrowAsync<IotHubServiceException>("Expected the request to fail with a \"not found\" error");
                error2.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
                error2.And.ErrorCode.Should().Be(IotHubErrorCode.DeviceNotFound);
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device1.Id).ConfigureAwait(false);
                    await serviceClient.Devices.DeleteAsync(device2.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddDeviceWithProxy()
        {
            string deviceId = _idPrefix + Guid.NewGuid();
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            var device = new Device(deviceId);
            await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
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
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                // Create a device to house the modules
                device = await serviceClient.Devices.CreateAsync(new Device(testDeviceId)).ConfigureAwait(false);

                // Create the modules on the device
                for (int i = 0; i < moduleCount; i++)
                {
                    Module createdModule = await serviceClient.Modules.CreateAsync(
                        new Module(testDeviceId, testModuleIds[i])).ConfigureAwait(false);
                }

                // Give the hub a moment
                await Task.Delay(250).ConfigureAwait(false);

                // List the modules on the test device
                IEnumerable<Module> modulesOnDevice =
                    await serviceClient.Devices.GetModulesAsync(testDeviceId).ConfigureAwait(false);

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
                await CleanupAsync(serviceClient, testDeviceId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test basic lifecycle of a module.
        /// This test includes CRUD operations only.
        /// </summary>
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ModulesClient_IdentityLifecycle()
        {
            string testDeviceId = $"IdentityLifecycleDevice{Guid.NewGuid()}";
            string testModuleId = $"IdentityLifecycleModule{Guid.NewGuid()}";

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            // Create a device to house the module
            Device device = await serviceClient.Devices.CreateAsync(new Device(testDeviceId)).ConfigureAwait(false);

            try
            {
                // Create a module on the device
                Module createdModule = await serviceClient.Modules.CreateAsync(
                    new Module(testDeviceId, testModuleId)).ConfigureAwait(false);

                createdModule.DeviceId.Should().Be(testDeviceId);
                createdModule.Id.Should().Be(testModuleId);

                // Get device
                // Get the device and compare ETag values (should remain unchanged);
                Module retrievedModule = await serviceClient.Modules.GetAsync(testDeviceId, testModuleId).ConfigureAwait(false);

                retrievedModule.Should().NotBeNull($"When checking for ETag, got null back for GET on module '{testDeviceId}/{testModuleId}'.");
                retrievedModule.ETag.Should().Be(createdModule.ETag, "ETag value should not have changed between create and get.");

                // Update a module
                string managedByValue = "SomeChangedValue";
                retrievedModule.ManagedBy = managedByValue;

                Module updatedModule = await serviceClient.Modules.SetAsync(retrievedModule).ConfigureAwait(false);

                updatedModule.ManagedBy.Should().Be(managedByValue, "Module should have changed its managedBy value");

                // Delete the device
                // Deleting the device happens in the finally block as cleanup.
            }
            finally
            {
                await CleanupAsync(serviceClient, testDeviceId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Test basic operations of a module's twin.
        /// </summary>
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task RegistryManager_DeviceTwinLifecycle()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            TestModule module = await TestModule.GetTestModuleAsync(_idPrefix, _idPrefix, Logger).ConfigureAwait(false);

            try
            {
                // Get the module twin
                Twin moduleTwin = await serviceClient.Twins.GetAsync(module.DeviceId, module.Id).ConfigureAwait(false);

                moduleTwin.ModuleId.Should().BeEquivalentTo(module.Id, "ModuleId on the Twin should match that of the module identity.");

                // Update device twin
                string propName = "username";
                string propValue = "userA";
                moduleTwin.Properties.Desired[propName] = propValue;

                Twin updatedModuleTwin = await serviceClient.Twins.UpdateAsync(module.DeviceId, module.Id, moduleTwin).ConfigureAwait(false);

                Assert.IsNotNull(updatedModuleTwin.Properties.Desired[propName]);
                Assert.AreEqual(propValue, (string)updatedModuleTwin.Properties.Desired[propName]);

                // Delete the module
                // Deleting the module happens in the finally block as cleanup.
            }
            finally
            {
                await CleanupAsync(serviceClient, module.DeviceId).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_GetStatistics()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            // No great way to test the accuracy of these statistics, but making the request successfully should
            // be enough to indicate that this API works as intended
            ServiceStatistics serviceStatistics = await serviceClient.Devices.GetServiceStatisticsAsync().ConfigureAwait(false);
            serviceStatistics.ConnectedDeviceCount.Should().BeGreaterOrEqualTo(0);

            // No great way to test the accuracy of these statistics, but making the request successfully should
            // be enough to indicate that this API works as intended
            RegistryStatistics registryStatistics = await serviceClient.Devices.GetRegistryStatisticsAsync().ConfigureAwait(false);
            registryStatistics.DisabledDeviceCount.Should().BeGreaterOrEqualTo(0);
            registryStatistics.EnabledDeviceCount.Should().BeGreaterOrEqualTo(0);
            registryStatistics.TotalDeviceCount.Should().BeGreaterOrEqualTo(0);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_SetDevicesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var device = new Device(_idPrefix + Guid.NewGuid());
            device = await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);

            try
            {
                ETag oldEtag = device.ETag;

                device.Status = DeviceStatus.Disabled;

                // Update the device once so that the last ETag falls out of date.
                device = await serviceClient.Devices.SetAsync(device).ConfigureAwait(false);

                // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
                // header appropriately when sending the request.
                device.ETag = oldEtag;

                // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
                Func<Task> act = async () => await serviceClient.Devices.SetAsync(device, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a device with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                FluentActions
                .Invoking(async () => { device = await serviceClient.Devices.SetAsync(device, false).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

                // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
                device.Status = DeviceStatus.Enabled;
                FluentActions
                .Invoking(async () => { device = await serviceClient.Devices.SetAsync(device, true).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device.Id).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_DeleteDevicesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var device = new Device(_idPrefix + Guid.NewGuid());
            device = await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);

            try
            {
                ETag oldEtag = device.ETag;

                device.Status = DeviceStatus.Disabled;

                // Update the device once so that the last ETag falls out of date.
                device = await serviceClient.Devices.SetAsync(device).ConfigureAwait(false);

                // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
                // header appropriately when sending the request.
                device.ETag = oldEtag;

                // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
                Func<Task> act = async () => await serviceClient.Devices.DeleteAsync(device, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a device with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                FluentActions
                .Invoking(async () => { await serviceClient.Devices.DeleteAsync(device, false).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");
            }
            finally
            {
                try
                {
                    await serviceClient.Devices.DeleteAsync(device.Id).ConfigureAwait(false);
                }
                catch (IotHubServiceException ex)
                    when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubErrorCode.DeviceNotFound)
                {
                    // device was already deleted during the normal test flow
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ModulesClient_SetModulesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            string deviceId = _idPrefix + Guid.NewGuid();
            string moduleId = _idPrefix + Guid.NewGuid();
            var module = new Module(deviceId, moduleId);
            Device device = await serviceClient.Devices.CreateAsync(new Device(deviceId)).ConfigureAwait(false);
            module = await serviceClient.Modules.CreateAsync(module).ConfigureAwait(false);
            module = await serviceClient.Modules.GetAsync(deviceId, moduleId).ConfigureAwait(false);

            try
            {
                ETag oldEtag = module.ETag;

                module.ManagedBy = "test";

                // Update the device once so that the last ETag falls out of date.
                module = await serviceClient.Modules.SetAsync(module).ConfigureAwait(false);

                // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
                // header appropriately when sending the request.
                module.ETag = oldEtag;

                // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
                Func<Task> act = async () => await serviceClient.Modules.SetAsync(module, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a module with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                FluentActions
                .Invoking(async () => { module = await serviceClient.Modules.SetAsync(module, false).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

                // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
                module.ManagedBy = "";
                device.Status = DeviceStatus.Enabled;
                FluentActions
                .Invoking(async () => { await serviceClient.Modules.SetAsync(module, true).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
            }
            finally
            {
                try
                {
                    await serviceClient.Modules.DeleteAsync(deviceId, moduleId).ConfigureAwait(false);
                    await CleanupAsync(serviceClient, deviceId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up module due to {ex}");
                }
            }
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ModulesClient_DeleteModulesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            string deviceId = _idPrefix + Guid.NewGuid();
            string moduleId = _idPrefix + Guid.NewGuid();
            var module = new Module(deviceId, moduleId);
            Device device = await serviceClient.Devices.CreateAsync(new Device(deviceId)).ConfigureAwait(false);
            module = await serviceClient.Modules.CreateAsync(module).ConfigureAwait(false);

            await RetryOperationHelper
                .RetryOperationsAsync(
                    async () =>
                    {
                        module = await serviceClient.Modules.GetAsync(deviceId, moduleId).ConfigureAwait(false);
                    },
                    RetryOperationHelper.DefaultRetryPolicy,
                    s_retryableExceptions,
                    Logger)
                .ConfigureAwait(false);

            try
            {
                ETag oldEtag = module.ETag;

                module.ManagedBy = "test";

                // Update the device once so that the last ETag falls out of date.
                module = await serviceClient.Modules.SetAsync(module).ConfigureAwait(false);

                // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
                // header appropriately when sending the request.
                module.ETag = oldEtag;

                // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
                Func<Task> act = async () => await serviceClient.Modules.DeleteAsync(module, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a module with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                FluentActions
                .Invoking(async () => { await serviceClient.Modules.DeleteAsync(module, false).ConfigureAwait(false); })
                .Should()
                .NotThrow<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");
            }
            finally
            {
                try
                {
                    await serviceClient.Modules.DeleteAsync(deviceId, moduleId).ConfigureAwait(false);
                    await CleanupAsync(serviceClient, deviceId).ConfigureAwait(false);
                }
                catch (IotHubServiceException ex)
                    when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubErrorCode.DeviceNotFound)
                {
                    // device was already deleted during the normal test flow
                }
                catch (Exception ex)
                {
                    Logger.Trace($"Failed to clean up module due to {ex}");
                }
            }
        }

        private static async Task CleanupAsync(IotHubServiceClient serviceClient, string deviceId)
        {
            if (deviceId == null)
            {
                return;
            }

            // cleanup
            try
            {
                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test clean up of device {deviceId} failed due to {ex}.");
            }
        }
    }
}
