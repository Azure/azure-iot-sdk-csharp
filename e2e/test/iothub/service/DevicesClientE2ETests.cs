// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using FluentAssertions;
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
    public class DevicesClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(DevicesClientE2ETests)}_";

        private static readonly HashSet<IotHubServiceErrorCode> s_getRetryableStatusCodes = new()
        {
            IotHubServiceErrorCode.DeviceNotFound,
            IotHubServiceErrorCode.ModuleNotFound,
        };
        private static readonly IIotHubServiceRetryPolicy s_retryPolicy = new HubServiceTestRetryPolicy(s_getRetryableStatusCodes);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
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
            Func<Task> act = async () => await serviceClient.Devices.GetAsync("device-that-does-not-exist").ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.Unknown);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddAndRemoveDeviceWithScope()
        {
            // arrange

            string edgeId1 = _idPrefix + Guid.NewGuid();
            string edgeId2 = _idPrefix + Guid.NewGuid();
            string deviceId = _idPrefix + Guid.NewGuid();

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            try
            {
                // act

                // Create a top-level edge device.
                var edgeDevice1 = new Device(edgeId1)
                {
                    Capabilities = new ClientCapabilities { IsIotEdge = true }
                };
                edgeDevice1 = await serviceClient.Devices.CreateAsync(edgeDevice1).ConfigureAwait(false);

                // Create a second-level edge device with edge 1 as the parent.
                var edgeDevice2 = new Device(edgeId2)
                {
                    Capabilities = new ClientCapabilities { IsIotEdge = true },
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _idPrefix + Guid.NewGuid();

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            var twin = new ClientTwin(deviceId)
            {
                Tags = { { "companyId", 1234 } },
            };

            var iotEdgeDevice = new Device(deviceId)
            {
                Capabilities = new ClientCapabilities { IsIotEdge = true }
            };

            await serviceClient.Devices.CreateWithTwinAsync(iotEdgeDevice, twin).ConfigureAwait(false);

            try
            {
                Device actual = null;
                do
                {
                    // allow some time for the device registry to update the cache
                    await Task.Delay(50).ConfigureAwait(false);
                    actual = await serviceClient.Devices.GetAsync(deviceId).ConfigureAwait(false);
                } while (actual == null);
                actual.Should().NotBeNull($"Got null in GET on device {deviceId} to check IotEdge property.");
                actual.Capabilities.IsIotEdge.Should().BeTrue();
            }
            finally
            {
                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_AddDevicesAsync_Works()
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

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            try
            {
                // act
                BulkRegistryOperationResult bulkAddResult = await serviceClient.Devices
                    .CreateAsync(new List<Device> { edge, device })
                    .ConfigureAwait(false);

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
                    VerboseTestLogger.WriteLine($"Failed to clean up devices due to {ex}");
                }
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_UpdateDevicesAsync_Works()
        {
            // arrange

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            var newStatus = ClientStatus.Disabled;
            testDevice1.Device.Status.Should().NotBe(newStatus);
            testDevice2.Device.Status.Should().NotBe(newStatus);

            // act

            testDevice1.Device.Status = testDevice2.Device.Status = newStatus;
            BulkRegistryOperationResult result = await serviceClient.Devices
                .SetAsync(new[] { testDevice1.Device, testDevice2.Device })
                .ConfigureAwait(false);

            // assert

            result.IsSuccessful.Should().BeTrue();

            Device actualDevice1 = await serviceClient.Devices.GetAsync(testDevice1.Id).ConfigureAwait(false);
            actualDevice1.Status.Should().Be(newStatus);

            Device actualDevice2 = await serviceClient.Devices.GetAsync(testDevice2.Id).ConfigureAwait(false);
            actualDevice2.Status.Should().Be(newStatus);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_RemoveDevicesAsync_Works()
        {
            // arrange

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());

            device1 = await serviceClient.Devices.CreateAsync(device1).ConfigureAwait(false);
            device2 = await serviceClient.Devices.CreateAsync(device2).ConfigureAwait(false);

            // act
            BulkRegistryOperationResult bulkDeleteResult = await serviceClient.Devices
                .DeleteAsync(new[] { device1, device2 }, false, default)
                .ConfigureAwait(false);

            // assert

            bulkDeleteResult.IsSuccessful.Should().BeTrue();

            Func<Task> act1 = async () => await serviceClient.Devices.GetAsync(device1.Id).ConfigureAwait(false);

            var error1 = await act1.Should().ThrowAsync<IotHubServiceException>("Expected the request to fail with a \"not found\" error");
            error1.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error1.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);

            Func<Task> act2 = async () => await serviceClient.Devices.GetAsync(device2.Id).ConfigureAwait(false);

            var error2 = await act2.Should().ThrowAsync<IotHubServiceException>("Expected the request to fail with a \"not found\" error");
            error2.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error2.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Proxy")]
        public async Task DevicesClient_AddDeviceWithProxy()
        {
            string deviceId = _idPrefix + Guid.NewGuid();
            var options = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString, options);
            var device = new Device(deviceId);
            try
            {
                await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);
            }
            finally
            {
                // clean up
                // If this fails, we shall let it throw an exception and fail the test
                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_SetDevicesETag_Works()
        {
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);

            ETag oldEtag = testDevice.Device.ETag;

            testDevice.Device.Status = ClientStatus.Disabled;

            // Update the device once so that the last ETag falls out of date.
            testDevice.Device = await serviceClient.Devices.SetAsync(testDevice.Device).ConfigureAwait(false);

            // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
            // header appropriately when sending the request.
            testDevice.Device.ETag = oldEtag;

            // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
            Func<Task> act = async () => await serviceClient.Devices.SetAsync(testDevice.Device, true).ConfigureAwait(false);
            var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a device with an out of date ETag");
            error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
            error.And.IsTransient.Should().BeFalse();

            // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
            await FluentActions
                .Invoking(async () => { testDevice.Device = await serviceClient.Devices.SetAsync(testDevice.Device, false).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

            // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
            testDevice.Device.Status = ClientStatus.Enabled;
            await FluentActions
                .Invoking(async () => { testDevice.Device = await serviceClient.Devices.SetAsync(testDevice.Device, true).ConfigureAwait(false); })
                .Should()
                .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task DevicesClient_DeleteDevicesETag_Works()
        {
            // arrange

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            var device = new Device(_idPrefix);
            device = await serviceClient.Devices.CreateAsync(device).ConfigureAwait(false);

            try
            {
                ETag oldEtag = device.ETag;

                // Update the device once so that the last ETag falls out of date.
                device.Status = ClientStatus.Disabled;
                device = await serviceClient.Devices.SetAsync(device).ConfigureAwait(false);

                // Deliberately set the ETag to an older version to test that the SDK is setting the If-Match
                // header appropriately when sending the request.
                device.ETag = oldEtag;

                // set the 'onlyIfUnchanged' flag to true to check that, with an out of date ETag, the request throws a PreconditionFailedException.
                Func<Task> act = async () => await serviceClient.Devices.DeleteAsync(device, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a device with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();
            }
            finally
            {
                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                await FluentActions
                    .Invoking(async () => { await serviceClient.Devices.DeleteAsync(device, false).ConfigureAwait(false); })
                    .Should()
                    .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");
            }
        }
    }
}
