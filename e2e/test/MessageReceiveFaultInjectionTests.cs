// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("IoTHub-FaultInjection")]
    public partial class MessageReceiveFaultInjectionTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageReceiveFaultInjectionTests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageReceiveFaultInjectionTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpConn, "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpSessionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpSessionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpC2D,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private Client.Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: payload='{payload}' p1Value='{p1Value}'");

            return new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
        }

        private async Task ReceiveMessageRecovery(
            TestDeviceType type,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec)
        {
            using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                Func<DeviceClient, TestDevice, Task> init = async (deviceClient, testDevice) =>
                {
                    await serviceClient.OpenAsync().ConfigureAwait(false);

                    if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                        transport == Client.TransportType.Mqtt_WebSocket_Only)
                    {
                        // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                        await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                };

                Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
                {
                    string payload, messageId, p1Value;
                    await serviceClient.SendAsync(
                        testDevice.Id,
                        MessageReceiveE2ETests.ComposeC2DTestMessage(out payload, out messageId, out p1Value)).ConfigureAwait(false);
                    await MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
                };

                Func<Task> cleanupOperation = () =>
                {
                    return serviceClient.CloseAsync();
                };

                await FaultInjection.TestErrorInjectionAsync(
                    DevicePrefix,
                    type,
                    transport,
                    faultType,
                    reason,
                    delayInSec,
                    FaultInjection.DefaultDurationInSec,
                    init,
                    testOperation,
                    cleanupOperation).ConfigureAwait(false);
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
