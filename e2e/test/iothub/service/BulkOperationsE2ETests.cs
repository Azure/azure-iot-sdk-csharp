// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
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

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task BulkOperations_UpdateTwins2Device_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            ClientTwin twin = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<ClientTwin> { twin }, false).ConfigureAwait(false);
            result.IsSuccessful.Should().BeTrue($"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            ClientTwin twinUpd = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);

            twin.DeviceId.Should().Be(twinUpd.DeviceId, "Device ID changed");
            twinUpd.Tags.Should().NotBeNull("Twin.Tags is null");
            twinUpd.Tags.ContainsKey(tagName).Should().BeTrue("Twin doesn't contain the tag");
            twin.Tags[tagName].Should().Be(twinUpd.Tags[tagName], "Tag value changed");
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task BulkOperations_UpdateTwins2DevicePatch_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            var twin = new ClientTwin(testDevice.Id);
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<ClientTwin> { twin }, false).ConfigureAwait(false);
            result.IsSuccessful.Should().BeTrue($"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            ClientTwin twinUpd = await serviceClient.Twins.GetAsync(testDevice.Id).ConfigureAwait(false);

            twin.DeviceId.Should().Be(twinUpd.DeviceId, "Device ID changed");
            twinUpd.Tags.Should().NotBeNull("Twin.Tags is null");
            twinUpd.Tags.ContainsKey(tagName).Should().BeTrue("Twin doesn't contain the tag");
            twin.Tags[tagName].Should().Be(twinUpd.Tags[tagName], "Tag value changed");
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task BulkOperations_UpdateTwins2Module_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            ClientTwin twin = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<ClientTwin> { twin }, false).ConfigureAwait(false);
            result.IsSuccessful.Should().BeTrue($"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            ClientTwin twinUpd = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            twin.DeviceId.Should().Be(twinUpd.DeviceId, "Device ID changed");
            twin.ModuleId.Should().Be(twinUpd.ModuleId, "Module ID changed");
            twinUpd.Tags.Should().NotBeNull("Twin.Tags is null");
            twinUpd.Tags.ContainsKey(tagName).Should().BeTrue("Twin doesn't contain the tag");
            twin.Tags[tagName].Should().Be(twinUpd.Tags[tagName], "Tag value changed");
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task BulkOperations_UpdateTwins2ModulePatch_Ok()
        {
            string tagName = Guid.NewGuid().ToString();
            string tagValue = Guid.NewGuid().ToString();

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var twin = new ClientTwin(testModule.DeviceId)
            {
                ModuleId = testModule.Id
            };
            twin.Tags[tagName] = tagValue;

            BulkRegistryOperationResult result = await serviceClient.Twins.UpdateAsync(new List<ClientTwin> { twin }, false).ConfigureAwait(false);
            result.IsSuccessful.Should().BeTrue($"UpdateTwins2Async error:\n{ResultErrorsToString(result)}");

            ClientTwin twinUpd = await serviceClient.Twins.GetAsync(testModule.DeviceId, testModule.Id).ConfigureAwait(false);

            twin.DeviceId.Should().Be(twinUpd.DeviceId, "Device ID changed");
            twin.ModuleId.Should().Be(twinUpd.ModuleId, "Module ID changed");
            twinUpd.Tags.Should().NotBeNull("Twin.Tags is null");
            twinUpd.Tags.ContainsKey(tagName).Should().BeTrue("Twin doesn't contain the tag");
            twin.Tags[tagName].Should().Be(twinUpd.Tags[tagName], "Tag value changed");
        }

        private static string ResultErrorsToString(BulkRegistryOperationResult result)
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
