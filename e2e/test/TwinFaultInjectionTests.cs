// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
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
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceReportedPropertiesGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateTcpConnRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }
        
        [Ignore] //TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Mqtt()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MqttWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Twin_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            var propName = Guid.NewGuid().ToString();
            var props = new TwinCollection();

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                var propValue = Guid.NewGuid().ToString();
                props[propName] = propValue;

                await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

                var deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
                Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);
            };

            await FaultInjection.TestErrorInjectionAsync(
                DevicePrefix,
                TestDeviceType.Sasl,
                transport,
                faultType,
                reason,
                delayInSec,
                FaultInjection.DefaultDurationInSec,
                (d, t) => { return Task.FromResult<bool>(false); },
                testOperation,
                () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
        }

        private async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twinPatch = new Twin();
                twinPatch.Properties.Desired[propName] = propValue;

                await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
                await registryManager.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
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

            // This is a simple hack to ensure that we're still exercising these tests while we address the race conditions causing the following issues:
            // [Ignore] // TODO: #571
            // [Ignore] // TODO: #558
            int retryCount = 3;

            while (retryCount-- > 0)
            {
                try
                {
                    await FaultInjection.TestErrorInjectionAsync(
                        DevicePrefix,
                        TestDeviceType.Sasl,
                        transport,
                        faultType,
                        reason,
                        delayInSec,
                        FaultInjection.DefaultDurationInSec,
                        initOperation,
                        testOperation,
                        () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
                }
                catch (DeviceNotFoundException e)
                {
                    s_log.WriteLine($"Retrying fault injection test ({retryCount} left)");
                }
            }
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
