// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
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
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            await using TestModule module = await TestModule.GetTestModuleAsync(_idPrefix, _idPrefix).ConfigureAwait(false);

            // Get the module twin
            ClientTwin moduleTwin = await serviceClient.Twins.GetAsync(module.DeviceId, module.Id).ConfigureAwait(false);

            moduleTwin.ModuleId.Should().Be(module.Id, "ModuleId on the Twin should match that of the module identity.");

            // Update device twin
            string propName = "username";
            string propValue = "userA";
            moduleTwin.Properties.Desired[propName] = propValue;

            ClientTwin updatedModuleTwin = await serviceClient.Twins.UpdateAsync(module.DeviceId, module.Id, moduleTwin).ConfigureAwait(false);

            ((string)updatedModuleTwin.Properties.Desired[propName]).Should().Be(propValue);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task TwinsClient_UpdateTwinsAsync_Works()
        {
            // arrange

            await using TestDevice testDevice1 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            IotHubDeviceClient device1 = testDevice1.CreateDeviceClient();
            await using TestDevice testDevice2 = await TestDevice.GetTestDeviceAsync(_idPrefix).ConfigureAwait(false);
            IotHubDeviceClient device2 = testDevice1.CreateDeviceClient();
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            ClientTwin twin1 = await serviceClient.Twins.GetAsync(testDevice1.Id).ConfigureAwait(false);
            ClientTwin twin2 = await serviceClient.Twins.GetAsync(testDevice2.Id).ConfigureAwait(false);

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

            ClientTwin actualTwin1 = await serviceClient.Twins.GetAsync(testDevice1.Id).ConfigureAwait(false);
            ClientTwin actualTwin2 = await serviceClient.Twins.GetAsync(testDevice2.Id).ConfigureAwait(false);

            ((string)actualTwin1.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
            ((string)actualTwin2.Properties.Desired[expectedProperty]).Should().Be(expectedPropertyValue);
        }
    }
}
