// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public class TwinFaultInjectionTests : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(TwinFaultInjectionTests)}_";

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecovery(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            string propName = Guid.NewGuid().ToString();
            var props = new Client.TwinCollection();

            async Task testOperation(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                string propValue = Guid.NewGuid().ToString();
                props[propName] = propValue;

                await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

                Client.Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(deviceTwin, $"{nameof(deviceTwin)} is null");
                Assert.IsNotNull(deviceTwin.Properties, $"{nameof(deviceTwin)}.Properties is null");
                Assert.IsNotNull(deviceTwin.Properties.Reported, $"{nameof(deviceTwin)}.Properties.Reported is null");
                Assert.IsNotNull(deviceTwin.Properties.Reported[propName], $"{nameof(deviceTwin)}.Properties.Reported[{nameof(propName)}] is null");
                Assert.AreEqual<string>(deviceTwin.Properties.Reported[propName].ToString(), propValue);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    proxyAddress,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultFaultDuration,
                    (d, t) => { return Task.FromResult(false); },
                    testOperation,
                    () => { return Task.FromResult(false); },
                    Logger)
                .ConfigureAwait(false);
        }

        private static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propName] = propValue;

            await serviceClient.Twins.UpdateAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var registryManager = RegistryManager.CreateFromConnectionString(TestConfiguration.IoTHub.ConnectionString);
            using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

            string propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            // Configure the callback and start accepting twin changes.
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAsync(propName).ConfigureAwait(false);
            }

            // Change the twin from the service side and verify the device received it.
            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                string propValue = Guid.NewGuid().ToString();
                testDeviceCallbackHandler.ExpectedTwinPropertyValue = propValue;

                Logger.Trace($"{nameof(Twin_DeviceDesiredPropertyUpdateRecoveryAsync)}: name={propName}, value={propValue}");

                Task serviceSendTask = TwinFaultInjectionTests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(cts.Token);

                var tasks = new List<Task>() { serviceSendTask, twinReceivedTask };
                while (tasks.Count > 0)
                {
                    Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                    completedTask.GetAwaiter().GetResult();
                    tasks.Remove(completedTask);
                }
            }

            // Cleanup references.
            Task CleanupOperationAsync()
            {
                testDeviceCallbackHandler?.Dispose();
                return Task.FromResult(false);
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    s_devicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    proxyAddress,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
