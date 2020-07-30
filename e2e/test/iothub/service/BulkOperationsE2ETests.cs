// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class BulkOperationsE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"E2E_{nameof(BulkOperationsE2ETests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(BulkOperationsE2ETests)}_";

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2Device_Ok()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Twin twin = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);

            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            var result = await registryManager.UpdateTwins2Async(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");

            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2DevicePatch_Ok()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Twin twin = new Twin();
            twin.DeviceId = testDevice.Id;
            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            var result = await registryManager.UpdateTwins2Async(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await registryManager.GetTwinAsync(testDevice.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");

            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2Module_Ok()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, Logger).ConfigureAwait(false);
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Twin twin = await registryManager.GetTwinAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            var result = await registryManager.UpdateTwins2Async(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await registryManager.GetTwinAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.AreEqual(twin.ModuleId, twinUpd.ModuleId, "Module ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");

            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2ModulePatch_Ok()
        {
            var tagName = Guid.NewGuid().ToString();
            var tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, Logger).ConfigureAwait(false);

            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            var twin = new Twin();
            twin.DeviceId = testModule.DeviceId;
            twin.ModuleId = testModule.Id;
            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            var result = await registryManager.UpdateTwins2Async(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await registryManager.GetTwinAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.AreEqual(twin.ModuleId, twinUpd.ModuleId, "Module ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");

            await registryManager.CloseAsync().ConfigureAwait(false);
        }

        private string ResultErrorsToString(BulkRegistryOperationResult result)
        {
            var errorString = "";

            foreach (var error in result.Errors)
            {
                errorString += $"\t{error.ErrorCode} : {error.ErrorStatus}\n";
            }

            return errorString;
        }
    }
}
