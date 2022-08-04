// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class BulkOperationsE2ETests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(BulkOperationsE2ETests)}_";
        private readonly string ModulePrefix = $"{nameof(BulkOperationsE2ETests)}_";

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2Device_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            Twin twin = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);

            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2DevicePatch_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            var twin = new Twin();
            twin.DeviceId = testDevice.Id;
            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2Module_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, Logger).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            Twin twin = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.AreEqual(twin.ModuleId, twinUpd.ModuleId, "Module ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");
        }

        [LoggedTestMethod]
        public async Task BulkOperations_UpdateTwins2ModulePatch_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix, Logger).ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            var twin = new Twin();
            twin.DeviceId = testModule.DeviceId;
            twin.ModuleId = testModule.Id;
            twin.Tags = new TwinCollection();
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<Twin> { twin }, true).ConfigureAwait(false);
            Assert.IsTrue(result.IsSuccessful, $"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            Twin twinUpd = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            Assert.AreEqual(twin.DeviceId, twinUpd.DeviceId, "Device ID changed");
            Assert.AreEqual(twin.ModuleId, twinUpd.ModuleId, "Module ID changed");
            Assert.IsNotNull(twinUpd.Tags, "Twin.Tags is null");
            Assert.IsTrue(twinUpd.Tags.Contains(tagName), "Twin doesn't contain the tag");
            Assert.AreEqual((string)twin.Tags[tagName], (string)twinUpd.Tags[tagName], "Tag value changed");
        }

        private string ResultErrorsToString(BulkRegistryOperationResult result)
        {
            string errorString = "";

            foreach (DeviceRegistryOperationError error in result.Errors)
            {
                errorString += $"\t{error.ErrorCode} : {error.ErrorStatus}\n";
            }

            return errorString;
        }
    }
}
