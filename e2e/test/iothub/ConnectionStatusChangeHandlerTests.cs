// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ConnectionStatusChangeHandlerTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(ConnectionStatusChangeHandlerTests)}_Device";
        private readonly string ModulePrefix = $"{nameof(ConnectionStatusChangeHandlerTests)}";

        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                    async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [TestCategory("LongRunning")]
        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        public async Task IotHubDeviceClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)] // This test always takes more than 5 minutes for service to return. Needs investigation.
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                    async (r, d) =>
                    {
                        Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                        device.Status = DeviceStatus.Disabled;
                        await r.Devices.SetAsync(device).ConfigureAwait(false);
                    })
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubDeviceClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                async (r, d) =>
                {
                    Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                    device.Status = DeviceStatus.Disabled;
                    await r.Devices.SetAsync(device).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(ConnectionStateChangeTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task IotHubModuleClient_DeviceDeleted_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    async (r, d) => await r.Devices.DeleteAsync(d).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpTcp()
        {
            await IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.Tcp),
                    async (r, d) =>
                    {
                        Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                        device.Status = DeviceStatus.Disabled;
                        await r.Devices.SetAsync(device).ConfigureAwait(false);
                    })
                .ConfigureAwait(false);
        }

        // IoT hub currently is somehow allowing new AMQP connections (encapsulated in a ModuleClient) even when the
        // device is disabled. This needs to be investigated and fixed. Once that's done, this test can be re-enabled.
        // [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task IotHubModuleClient_DeviceDisabled_Gives_ConnectionStatus_DeviceDisabled_AmqpWs()
        {
            await IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    async (r, d) =>
                    {
                        Device device = await r.Devices.GetAsync(d).ConfigureAwait(false);
                        device.Status = DeviceStatus.Disabled;
                        await r.Devices.SetAsync(device).ConfigureAwait(false);
                    })
                .ConfigureAwait(false);
        }

        private async Task IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubServiceClient, string, Task> registryManagerOperation)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, DevicePrefix + $"_{Guid.NewGuid()}").ConfigureAwait(false);
            string deviceConnectionString = testDevice.ConnectionString;

            var config = new TestConfiguration.IotHub.ConnectionStringParser(deviceConnectionString);
            string deviceId = config.DeviceId;

            ConnectionStatusInfo connectionStatusInfo = null;
            int deviceDisabledReceivedCount = 0;

            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(deviceConnectionString, options);
            void statusChangeHandler(ConnectionStatusInfo c)
            {
                if (c.ChangeReason == ConnectionStatusChangeReason.DeviceDisabled)
                {
                    connectionStatusInfo = c;
                    deviceDisabledReceivedCount++;
                }
            }

            deviceClient.SetConnectionStatusChangeHandler(statusChangeHandler);
            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Created {nameof(IotHubDeviceClient)} with device Id={testDevice.Id}");

            await deviceClient.OpenAsync().ConfigureAwait(false);

            // Receiving the module twin should succeed right now.
            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: DeviceClient GetTwinAsync.");
            Client.Twin twin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.IsNotNull(twin);

            // Delete/disable the device in IoT hub. This should trigger the ConnectionStatusChangeHandler.
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await registryManagerOperation(serviceClient, deviceId).ConfigureAwait(false);

            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection status change handler to get triggered.
            int sleepCount = 50;
            for (int i = 0; i < sleepCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (deviceDisabledReceivedCount == 1)
                {
                    break;
                }
            }

            Logger.Trace($"{nameof(IotHubDeviceClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Asserting connection status change.");

            deviceDisabledReceivedCount.Should().Be(1);
            deviceClient.ConnectionStatusInfo.Status.Should().Be(ConnectionStatus.Disconnected);
            deviceClient.ConnectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.DeviceDisabled);
            connectionStatusInfo.RecommendedAction.Should().Be(RecommendedAction.Quit);
        }

        private async Task IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubServiceClient, string, Task> registryManagerOperation)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix + $"_{Guid.NewGuid()}", ModulePrefix, Logger).ConfigureAwait(false);
            ConnectionStatusInfo connectionStatusInfo = null;
            int deviceDisabledReceivedCount = 0;
            Action<ConnectionStatusInfo> statusChangeHandler = (c) =>
            {
                if (c.ChangeReason == ConnectionStatusChangeReason.DeviceDisabled)
                {
                    connectionStatusInfo = c;
                    deviceDisabledReceivedCount++;
                }
            };
            var options = new IotHubClientOptions(transportSettings);

            using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            moduleClient.SetConnectionStatusChangeHandler(statusChangeHandler);
            Logger.Trace($"{nameof(IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Created {nameof(IotHubModuleClient)} with moduleId={testModule.Id}");

            await moduleClient.OpenAsync().ConfigureAwait(false);

            // Receiving the module twin should succeed right now.
            Logger.Trace($"{nameof(IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: ModuleClient GetTwinAsync.");
            Client.Twin twin = await moduleClient.GetTwinAsync().ConfigureAwait(false);
            Assert.IsNotNull(twin);

            // Delete/disable the device in IoT hub.
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            await registryManagerOperation(serviceClient, testModule.DeviceId).ConfigureAwait(false);

            Logger.Trace($"{nameof(IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Completed RegistryManager operation.");

            // Artificial sleep waiting for the connection status change handler to get triggered.
            int sleepCount = 50;
            for (int i = 0; i < sleepCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (deviceDisabledReceivedCount == 1)
                {
                    break;
                }
            }

            Logger.Trace($"{nameof(IotHubModuleClient_Gives_ConnectionStatus_Disconnected_ChangeReason_DeviceDisabled_Base)}: Asserting connection status change.");

            deviceDisabledReceivedCount.Should().Be(1);
            deviceClient.ConnectionStatusInfo.Status.Should().Be(ConnectionStatus.Disconnected);
            deviceClient.ConnectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.DeviceDisabled);
            connectionStatusInfo.RecommendedAction.Should().Be(RecommendedAction.Quit);
        }
    }
}
