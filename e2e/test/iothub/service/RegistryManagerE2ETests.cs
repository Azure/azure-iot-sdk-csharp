// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Iothub.Service
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class RegistryManagerE2ETests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"E2E_{nameof(RegistryManagerE2ETests)}_";

        [LoggedTestMethod]
        [TestCategory("Proxy")]
        [ExpectedException(typeof(Common.Exceptions.IotHubCommunicationException))]
        public async Task RegistryManager_BadProxy_ThrowsException()
        {
            // arrange
            var registryManager = RegistryManager.CreateFromConnectionString(
                Configuration.IoTHub.ConnectionString,
                new HttpTransportSettings
                {
                    Proxy = new WebProxy(Configuration.IoTHub.InvalidProxyServerAddress),
                });

            // act
            _ = await registryManager.GetDeviceAsync("device-that-does-not-exist").ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddAndRemoveDeviceWithScope()
        {
            var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            string deviceId = _devicePrefix + Guid.NewGuid();

            var edgeDevice = new Device(deviceId)
            {
                Capabilities = new DeviceCapabilities { IotEdge = true }
            };
            edgeDevice = await registryManager.AddDeviceAsync(edgeDevice).ConfigureAwait(false);

            var leafDevice = new Device(Guid.NewGuid().ToString()) { Scope = edgeDevice.Scope };
            Device receivedDevice = await registryManager.AddDeviceAsync(leafDevice).ConfigureAwait(false);

            Assert.IsNotNull(receivedDevice);
            Assert.AreEqual(leafDevice.Id, receivedDevice.Id, $"Expected Device ID={leafDevice.Id}; Actual Device ID={receivedDevice.Id}");
            Assert.AreEqual(leafDevice.Scope, receivedDevice.Scope, $"Expected Device Scope={leafDevice.Scope}; Actual Device Scope={receivedDevice.Scope}");
            await registryManager.RemoveDeviceAsync(leafDevice.Id).ConfigureAwait(false);
            await registryManager.RemoveDeviceAsync(edgeDevice.Id).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddDeviceWithTwinWithDeviceCapabilities()
        {
            string deviceId = _devicePrefix + Guid.NewGuid();

            using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twin = new Twin
                {
                    Tags = new TwinCollection(@"{ companyId: 1234 }"),
                };

                var iotEdgeDevice = new Device(deviceId)
                {
                    Capabilities = new DeviceCapabilities { IotEdge = true }
                };

                await registryManager.AddDeviceWithTwinAsync(iotEdgeDevice, twin).ConfigureAwait(false);

                Device actual = await registryManager.GetDeviceAsync(deviceId).ConfigureAwait(false);
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);

                Assert.IsTrue(actual.Capabilities.IotEdge);
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_AddDeviceWithProxy()
        {
            string deviceId = _devicePrefix + Guid.NewGuid();
            var transportSettings = new HttpTransportSettings
            {
                Proxy = new WebProxy(Configuration.IoTHub.ProxyServerAddress)
            };

            using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString, transportSettings))
            {
                var device = new Device(deviceId);
                await registryManager.AddDeviceAsync(device).ConfigureAwait(false);
            }
        }

        [LoggedTestMethod]
        public async Task RegistryManager_Query_Works()
        {
            // arrange
            using var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            string deviceId = $"{_devicePrefix}{Guid.NewGuid()}";

            try
            {
                Device device = await registryManager
                    .AddDeviceAsync(new Device(deviceId))
                    .ConfigureAwait(false);

                // act

                IQuery query = null;
                IEnumerable<Twin> twins = null;
                for (int i = 0; i < 30; ++i)
                {
                    string queryText = $"select * from devices where deviceId = '{deviceId}'";
                    query = registryManager.CreateQuery(queryText);

                    twins = await query.GetNextAsTwinAsync().ConfigureAwait(false);

                    if (twins.Count() > 0)
                    {
                        break;
                    }

                    // A new device may not return immediately from a query, so give it some time and some retries to appear
                    await Task.Delay(250).ConfigureAwait(false);
                }

                // assert
                twins.Count().Should().Be(1, "only asked for 1 device by its Id");
                twins.First().DeviceId.Should().Be(deviceId, "The Id of the device returned should match");
                query.HasMoreResults.Should().BeFalse("We've processed the single, expected result");
            }
            finally
            {
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
            }
        }
    }
}
