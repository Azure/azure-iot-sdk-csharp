// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class PayloadConventionE2eTest : E2EMsTestBase
    {
        private static readonly string _devicePrefix = $"{nameof(PayloadConventionE2eTest)}_";
        
        [TestMethod]
        public async Task Device_Twin_SystemTextJsonConvention_UpdateReportedProperties()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            PayloadConvention convention = SystemTextJsonPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            var expected = new DeviceTwinCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out DeviceTwinCustomProperty actual)
                .Should().BeTrue();
            actual.CustomProperty.Should().Be(expected.CustomProperty);
            actual.Guid.Should().Be(expected.Guid);
        }

        [TestMethod]
        public async Task Device_Twin_DefaultPayloadConvention_UpdateReportedProperties()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            PayloadConvention convention = DefaultPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            var expected = new DeviceTwinCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out DeviceTwinCustomProperty actual)
                .Should().BeTrue();
            actual.CustomProperty.Should().Be(expected.CustomProperty);
            actual.Guid.Should().Be(expected.Guid);
        }
    }
}
