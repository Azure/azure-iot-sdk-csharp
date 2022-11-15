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

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ModulesClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(ModulesClientE2ETests)}_";

        private static readonly HashSet<IotHubServiceErrorCode> s_getRetryableStatusCodes = new()
        {
            IotHubServiceErrorCode.DeviceNotFound,
            IotHubServiceErrorCode.ModuleNotFound,
        };
        private static readonly IIotHubServiceRetryPolicy s_retryPolicy = new HubServiceTestRetryPolicy(s_getRetryableStatusCodes);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
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
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
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
                Module retrievedModule = null;

                await RetryOperationHelper
                    .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        retrievedModule = await serviceClient.Modules.GetAsync(testDeviceId, testModuleId).ConfigureAwait(false);
                    },
                    s_retryPolicy)
                .ConfigureAwait(false);

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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ModulesClient_SetModulesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            string deviceId = _idPrefix + Guid.NewGuid();
            string moduleId = _idPrefix + Guid.NewGuid();
            var module = new Module(deviceId, moduleId);
            Device device = await serviceClient.Devices.CreateAsync(new Device(deviceId)).ConfigureAwait(false);
            module = await serviceClient.Modules.CreateAsync(module).ConfigureAwait(false);

            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        module = await serviceClient.Modules.GetAsync(deviceId, moduleId).ConfigureAwait(false);
                    },
                    s_retryPolicy)
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
                Func<Task> act = async () => await serviceClient.Modules.SetAsync(module, true).ConfigureAwait(false);
                var error = await act.Should().ThrowAsync<IotHubServiceException>("Expected test to throw a precondition failed exception since it updated a module with an out of date ETag");
                error.And.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
                error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                await FluentActions
                    .Invoking(async () => { module = await serviceClient.Modules.SetAsync(module, false).ConfigureAwait(false); })
                    .Should()
                    .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");

                // set the 'onlyIfUnchanged' flag to true to check that, with an up-to-date ETag, the request performs without exception.
                module.ManagedBy = "";
                device.Status = ClientStatus.Enabled;
                await FluentActions
                    .Invoking(async () => { await serviceClient.Modules.SetAsync(module, true).ConfigureAwait(false); })
                    .Should()
                    .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to true");
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
                    VerboseTestLogger.WriteLine($"Failed to clean up module due to {ex}");
                }
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task ModulesClient_DeleteModulesETag_Works()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            string deviceId = _idPrefix + Guid.NewGuid();
            string moduleId = _idPrefix + Guid.NewGuid();
            var module = new Module(deviceId, moduleId);
            Device device = await serviceClient.Devices.CreateAsync(new Device(deviceId)).ConfigureAwait(false);
            module = await serviceClient.Modules.CreateAsync(module).ConfigureAwait(false);

            await RetryOperationHelper
                .RunWithHubServiceRetryAsync(
                    async () =>
                    {
                        module = await serviceClient.Modules.GetAsync(deviceId, moduleId).ConfigureAwait(false);
                    },
                    s_retryPolicy)
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
                error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.PreconditionFailed);
                error.And.IsTransient.Should().BeFalse();

                // set the 'onlyIfUnchanged' flag to false to check that, even with an out of date ETag, the request performs without exception.
                await FluentActions
                    .Invoking(async () => { await serviceClient.Modules.DeleteAsync(module, false).ConfigureAwait(false); })
                    .Should()
                    .NotThrowAsync<IotHubServiceException>("Did not expect test to throw a precondition failed exception since 'onlyIfUnchanged' was set to false");
            }
            finally
            {
                try
                {
                    await serviceClient.Modules.DeleteAsync(deviceId, moduleId).ConfigureAwait(false);
                    await CleanupAsync(serviceClient, deviceId).ConfigureAwait(false);
                }
                catch (IotHubServiceException ex)
                    when (ex.StatusCode is HttpStatusCode.NotFound && ex.ErrorCode is IotHubServiceErrorCode.DeviceNotFound)
                {
                    // device was already deleted during the normal test flow
                }
                catch (Exception ex)
                {
                    VerboseTestLogger.WriteLine($"Failed to clean up module due to {ex}");
                }
            }
        }
    }
}
