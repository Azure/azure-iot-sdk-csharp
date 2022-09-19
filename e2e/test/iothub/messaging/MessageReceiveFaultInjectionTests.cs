// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public partial class MessageReceiveFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessageReceiveFaultInjectionTests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Mqtt()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_MqttWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Mqtt()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_MqttWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageWithCallbackRecoveryAsync(
            Client.TransportType transport,
            string faultType,
            string reason)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var serviceClient = ServiceClient.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                await serviceClient.OpenAsync().ConfigureAwait(false);

                await deviceClient.OpenAsync().ConfigureAwait(false);
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                (Message message, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);

                testDeviceCallbackHandler.ExpectedMessageSentByService = message;

                using (message)
                {
                    await serviceClient.SendAsync(testDevice.Id, message).ConfigureAwait(false);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
                }
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.CloseAsync().ConfigureAwait(false);
                testDeviceCallbackHandler?.Dispose();
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transport,
                    null,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
