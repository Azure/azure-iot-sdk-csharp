// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.iothub.service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class TwinsClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(TwinsClientE2ETests)}_";

        /// <summary>
        /// Test basic operations of a module's twin.
        /// </summary>
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinsClient_DeviceTwinLifecycle()
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            TestModule module = await TestModule.GetTestModuleAsync(_idPrefix, _idPrefix).ConfigureAwait(false);

            try
            {
                // Get the module twin
                ClientTwin moduleTwin = await serviceClient.Twins.GetAsync(module.DeviceId, module.Id).ConfigureAwait(false);

                moduleTwin.ModuleId.Should().Be(module.Id, "ModuleId on the Twin should match that of the module identity.");

                // Update device twin
                string propName = "username";
                string propValue = "userA";
                moduleTwin.Properties.Desired[propName] = propValue;

                ClientTwin updatedModuleTwin = await serviceClient.Twins.UpdateAsync(module.DeviceId, module.Id, moduleTwin).ConfigureAwait(false);

                ((string)updatedModuleTwin.Properties.Desired[propName]).Should().Be(propValue);

                // Deleting the module happens in the finally block as cleanup.
            }
            finally
            {
                await CleanupAsync(serviceClient, module.DeviceId).ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinsClient_UpdateTwinsAsync_Works()
        {
            // arrange

            var device1 = new Device(_idPrefix + Guid.NewGuid());
            var device2 = new Device(_idPrefix + Guid.NewGuid());
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            try
            {
                await serviceClient.Devices.CreateAsync(device1).ConfigureAwait(false);
                ClientTwin twin1 = await serviceClient.Twins.GetAsync(device1.Id).ConfigureAwait(false);
                await serviceClient.Devices.CreateAsync(device2).ConfigureAwait(false);
                ClientTwin twin2 = await serviceClient.Twins.GetAsync(device2.Id).ConfigureAwait(false);

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

                ClientTwin actualTwin1 = await serviceClient.Twins.GetAsync(device1.Id).ConfigureAwait(false);
                ((string)actualTwin1.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
                ClientTwin actualTwin2 = await serviceClient.Twins.GetAsync(device2.Id).ConfigureAwait(false);
                ((string)actualTwin2.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
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
                    VerboseTestLogger.WriteLine($"Failed to clean up devices due to {ex}");
                }
            }
        }
    }
}
