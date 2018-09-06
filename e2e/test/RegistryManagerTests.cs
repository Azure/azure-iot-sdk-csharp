// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class RegistryManagerTests
    {
        private static readonly string ConnectionString = Configuration.IoTHub.ConnectionString;

        [TestMethod]
        public async Task RegistryManager_AddAndRemoveDeviceWithScope()
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(ConnectionString);

            var edgeDevice = new Device(Guid.NewGuid().ToString())
            {
                Capabilities = new DeviceCapabilities { IotEdge = true }
            };
            edgeDevice = await registryManager.AddDeviceAsync(edgeDevice).ConfigureAwait(false);

            var leafDevice = new Device(Guid.NewGuid().ToString()) { Scope = edgeDevice.Scope };
            Device receivedDevice = await registryManager.AddDeviceAsync(leafDevice).ConfigureAwait(false);

            Assert.IsNotNull(receivedDevice);
            Assert.AreEqual(leafDevice.Id, receivedDevice.Id);
            Assert.AreEqual(leafDevice.Scope, receivedDevice.Scope);
            await registryManager.RemoveDeviceAsync(leafDevice.Id).ConfigureAwait(false);
            await registryManager.RemoveDeviceAsync(edgeDevice.Id).ConfigureAwait(false);
        }
    }
}