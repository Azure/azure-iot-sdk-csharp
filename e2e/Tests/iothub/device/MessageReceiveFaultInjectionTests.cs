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
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    public partial class MessageReceiveFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(MessageReceiveFaultInjectionTests)}_";

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Mqtt()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_MqttWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Mqtt()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_MqttWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_Amqp()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_Amqp()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossReceiveWithCallbackRecovery_AmqpWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2dLinkDropReceiveWithCallbackRecovery_Amqp()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpC2dLinkDropReceiveWithCallbackRecovery_AmqpWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_Amqp()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownReceiveWithCallbackRecovery_AmqpWs()
        {
            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageWithCallbackRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                await serviceClient.Messages.OpenAsync().ConfigureAwait(false);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);

                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
                await testDeviceCallbackHandler.SetMessageReceiveCallbackHandlerAndCompleteMessageAsync<string>().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                OutgoingMessage message = OutgoingMessageHelper.ComposeOutgoingTestMessage(out string payload, out string p1Value);

                testDeviceCallbackHandler.ExpectedOutgoingMessage = message;

                await serviceClient.Messages.SendAsync(testDevice.Id, message).ConfigureAwait(false);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await testDeviceCallbackHandler.WaitForReceiveMessageCallbackAsync(cts.Token).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync()
            {
                await serviceClient.Messages.CloseAsync().ConfigureAwait(false);
                testDeviceCallbackHandler?.Dispose();
            }

            await FaultInjection
                .TestErrorInjectionAsync(
                    DevicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    null,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync)
                .ConfigureAwait(false);
        }
    }
}
