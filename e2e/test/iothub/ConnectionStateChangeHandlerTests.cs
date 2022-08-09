// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ConnectionStateChangeHandlerTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(ConnectionStateChangeHandlerTests)}_Device";
        private readonly string ModulePrefix = $"{nameof(ConnectionStateChangeHandlerTests)}";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDeleted_Gives_ConnectionState_DeviceDisabled_AmqpTcp()
        {
            await IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [TestCategory("LongRunning")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_DeviceDeleted_Gives_ConnectionState_DeviceDisabled_AmqpWs()
        {
            await IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDisabled_Gives_ConnectionState_DeviceDisabled_AmqpTcp()
        {
            await IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                async (r, d) =>
                {
                    Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.Devices.SetAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDisabled_Gives_ConnectionState_DeviceDisabled_AmqpWs()
        {
            await IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                async (r, d) =>
                {
                    Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.Devices.SetAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionState_DeviceDisabled_AmqpTcp()
        {
            await ModuleClient_Gives_ConnectionState_DeviceDisabled_Base(
                IotHubClientTransportProtocol.Tcp, async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task ModuleClient_DeviceDeleted_Gives_ConnectionState_DeviceDisabled_AmqpWs()
        {
            await ModuleClient_Gives_ConnectionState_DeviceDisabled_Base(
                IotHubClientTransportProtocol.WebSocket, async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false)).ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionState_DeviceDisabled_AmqpTcp()
        {
            await ModuleClient_Gives_ConnectionState_DeviceDisabled_Base(
                IotHubClientTransportProtocol.Tcp, async (r, d) =>
                {
                    Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.Devices.SetAsync(device).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ModuleClient_DeviceDisabled_Gives_ConnectionState_DeviceDisabled_AmqpWs()
        {
            await ModuleClient_Gives_ConnectionState_DeviceDisabled_Base(
                IotHubClientTransportProtocol.WebSocket, async (r, d) =>
            {
                Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                device.Status = DeviceStatus.Disabled;
                await r.Devices.SetAsync(device).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubServiceClient, string, Task> registryManagerOperation)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IoTHub.ConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceID;

            ConnectionState? state = null;
            ConnectionStateChangeReason? stateChangeReason = null;
            int deviceDisabledReceivedCount = 0;

            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = IotHubDeviceClient.CreateFromConnectionString(deviceConnectionString, options);
            void stateChangeHandler(ConnectionState s, ConnectionStateChangeReason r)
            {
                if (r == ConnectionStateChangeReason.DeviceDisabled)
                {
                    state = s;
                    stateChangeReason = r;
                    deviceDisabledReceivedCount++;
                }
            }

            deviceClient.SetConnectionStateChangeHandler(stateChangeHandler);
            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base)}: Created {nameof(IotHubDeviceClient)} with device Id={testDevice.Id}");

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Receiving the module twin should succeed right now.
            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base)}: DeviceClient GetTwinAsync.");
            Client.Twin twin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.IsNotNull(twin);

            // Delete/disable the device in IoT hub. This should trigger the ConnectionStateChangeHandler.
            using (var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString))
            {
                await registryManagerOperation(serviceClient, deviceId).ConfigureAwait(false);
            }

            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection state change handler to get triggered.
            int sleepCount = 50;
            for (int i = 0; i < sleepCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (deviceDisabledReceivedCount == 1)
                {
                    break;
                }
            }

            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionState_DeviceDisabled_Base)}: Asserting connection state change.");

            Assert.AreEqual(1, deviceDisabledReceivedCount);
            Assert.AreEqual(ConnectionState.Disconnected, state);
            Assert.AreEqual(ConnectionStateChangeReason.DeviceDisabled, stateChangeReason);
        }

        private async Task ModuleClient_Gives_ConnectionState_DeviceDisabled_Base(
            IotHubClientTransportProtocol protocol,
            Func<IotHubServiceClient, string, Task> registryManagerOperation)
        {
            var transportSettings = new IotHubClientAmqpSettings(protocol);

            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix, Logger).ConfigureAwait(false);
            ConnectionState? state = null;
            ConnectionStateChangeReason? stateChangeReason = null;
            int deviceDisabledReceivedCount = 0;
            ConnectionStateChangeHandler stateChangeHandler = (s, r) =>
            {
                if (r == ConnectionStateChangeReason.DeviceDisabled)
                {
                    state = s;
                    stateChangeReason = r;
                    deviceDisabledReceivedCount++;
                }
            };
            var options = new IotHubClientOptions(transportSettings);

            using var moduleClient = IotHubModuleClient.CreateFromConnectionString(testModule.ConnectionString, options);
            moduleClient.SetConnectionStateChangeHandler(stateChangeHandler);
            Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionState_DeviceDisabled_Base)}: Created {nameof(IotHubModuleClient)} with moduleId={testModule.Id}");

            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Receiving the module twin should succeed right now.
            Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionState_DeviceDisabled_Base)}: ModuleClient GetTwinAsync.");
            Client.Twin twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);
            Assert.IsNotNull(twin);

            // Delete/disable the device in IoT hub.
            using (var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString))
            {
                await registryManagerOperation(serviceClient, testModule.DeviceId).ConfigureAwait(false);
            }

            Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionState_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection state change handler to get triggered.
            int sleepCount = 50;
            for (int i = 0; i < sleepCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (deviceDisabledReceivedCount == 1)
                {
                    break;
                }
            }

            Logger.Trace($"{nameof(ModuleClient_Gives_ConnectionState_DeviceDisabled_Base)}: Asserting connection state change.");

            Assert.AreEqual(1, deviceDisabledReceivedCount);
            Assert.AreEqual(ConnectionState.Disconnected, state);
            Assert.AreEqual(ConnectionStateChangeReason.DeviceDisabled, stateChangeReason);
        }
    }
}
