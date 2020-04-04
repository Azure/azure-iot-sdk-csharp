﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class TwinFaultInjectionTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(TwinFaultInjectionTests)}_";
        private static TestLogging s_log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public TwinFaultInjectionTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecovery(
                Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                var propValue = Guid.NewGuid().ToString();
                props[propName] = propValue;

                await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

                Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.IsNotNull(deviceTwin, $"{nameof(deviceTwin)} is null");
                Assert.IsNotNull(deviceTwin.Properties, $"{nameof(deviceTwin)}.Properties is null");
                Assert.IsNotNull(deviceTwin.Properties.Reported, $"{nameof(deviceTwin)}.Properties.Reported is null");
                Assert.IsNotNull(deviceTwin.Properties.Reported[propName], $"{nameof(deviceTwin)}.Properties.Reported[{nameof(propName)}] is null");
                Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);
            };

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transport,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultDurationInSec,
                    (d, t) => { return Task.FromResult<bool>(false); },
                    testOperation,
                    () => { return Task.FromResult<bool>(false); })
                .ConfigureAwait(false);
        }

        private async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using (var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twinPatch = new Twin();
                twinPatch.Properties.Desired[propName] = propValue;

                await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
                await registryManager.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecovery(
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            var registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            var cts = new CancellationTokenSource(FaultInjection.RecoveryTimeMilliseconds);

            var propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            // Configure the callback and start accepting twin changes.
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient);
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAsync(propName).ConfigureAwait(false);
            };

            // Change the twin from the service side and verify the device received it.
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                var propValue = Guid.NewGuid().ToString();
                testDeviceCallbackHandler.ExpectedTwinPropertyValue = propValue;

                s_log.WriteLine($"{nameof(Twin_DeviceDesiredPropertyUpdateRecovery)}: name={propName}, value={propValue}");

                Task serviceSendTask = RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(cts.Token);

                var tasks = new List<Task>() { serviceSendTask, twinReceivedTask };
                while (tasks.Count > 0)
                {
                    Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                    completedTask.GetAwaiter().GetResult();
                    tasks.Remove(completedTask);
                }
            };

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transport,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultDurationInSec,
                    initOperation,
                    testOperation,
                    () => { return Task.FromResult(false); })
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
