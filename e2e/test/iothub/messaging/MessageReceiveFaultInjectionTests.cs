// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("IoTHub")]
    [TestCategory("FaultInjection")]
    public partial class MessageReceiveFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessageReceiveFaultInjectionTests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientMqttSettings(),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay
                ).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpConn, "",
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_AmqpC2D,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientMqttSettings(),
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(
                TestDeviceType.Sasl,
                new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultFaultDelay).ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Mqtt()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_MqttWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Mqtt()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_MqttWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_AmqpConn, "",
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2DLinkDropReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Amqp()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_AmqpWs()
        {
            await
                ReceiveMessageWithCallbackRecoveryAsync(
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageRecovery(
            TestDeviceType type,
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);
            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                (Message message, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                await serviceClient.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);
                await MessageReceiveE2ETests.VerifyReceivedC2dMessageAsync(deviceClient, testDevice.Id, message, payload, Logger).ConfigureAwait(false);
            }

            Task CleanupOperationAsync()
            {
                return serviceClient.Messages.CloseAsync();
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    type,
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

        private async Task ReceiveMessageWithCallbackRecoveryAsync(
            TestDeviceType type,
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan delayInSec,
            string proxyAddress = null)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, Logger);
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                var timeout = TimeSpan.FromSeconds(20);
                using var cts = new CancellationTokenSource(timeout);
                (Message message, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);

                testDeviceCallbackHandler.ExpectedMessageSentByService = message;
                await serviceClient.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);

                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.Messages.CloseAsync();
                testDeviceCallbackHandler?.Dispose();
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    type,
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
