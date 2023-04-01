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
    public partial class IncomingMessageCallbackFaultInjectionTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(IncomingMessageCallbackFaultInjectionTests)}_";

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_TcpConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_GracefulShutdownRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientMqttSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_TcpConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_GracefulShutdownRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_AmqpConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_AmqpSessionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task IncomingMessage_AmqpC2dLinkDropRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageWithCallbackRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpC2D,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageWithCallbackRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);
                await testDevice.DeviceClient.OpenAsync(ct).ConfigureAwait(false);

                await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                OutgoingMessage message = OutgoingMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                testDeviceCallbackHandler.ExpectedOutgoingMessage = message;

                await serviceClient.Messages.SendAsync(testDevice.Id, message, ct).ConfigureAwait(false);
                await testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(CancellationToken ct)
            {
                await serviceClient.Messages.CloseAsync(ct).ConfigureAwait(false);
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
                    CleanupOperationAsync,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
