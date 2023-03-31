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
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class IncomingMessageeE2EPoolAmqpTests : E2EMsTestBase
    {
        private readonly string DevicePrefix = $"{nameof(IncomingMessageeE2EPoolAmqpTests)}_";

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.IotHub)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.IotHub)]
        public async Task IncomingMessage_ReceiveSingleMessage_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, ConnectionStringAuthScope authScope)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessage_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.Device)]
        [DataRow(IotHubClientTransportProtocol.Tcp, ConnectionStringAuthScope.IotHub)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, ConnectionStringAuthScope.IotHub)]
        public async Task IncomingMessage_ReceiveSingleMessageAndUnsubscribe_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, ConnectionStringAuthScope authScope)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await ReceiveMessageAndUnsubscribe_PoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessage_PoolOverAmqpAsync(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            CancellationToken ct)
        {
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await TestDevice.ServiceClient.Messages.OpenAsync(ct).ConfigureAwait(false);
                await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                VerboseTestLogger.WriteLine($"{nameof(IncomingMessageeE2EPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");

                OutgoingMessage msg = OutgoingMessageHelper.ComposeTestMessage(out string payload, out string _);
                testDeviceCallbackHandler.ExpectedOutgoingMessage = msg;

                await TestDevice.ServiceClient.Messages.SendAsync(testDevice.Id, msg, ct).ConfigureAwait(false);
                await testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    null,
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageAndUnsubscribe_PoolOverAmqpAsync(
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            CancellationToken ct)
        {
            async Task InitOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                await TestDevice.ServiceClient.Messages.OpenAsync(ct).ConfigureAwait(false);
                await testDeviceCallbackHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<string>(ct).ConfigureAwait(false);
            }

            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler, CancellationToken ct)
            {
                // Send a message to the device from the service.
                OutgoingMessage firstMessage = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
                testDeviceCallbackHandler.ExpectedOutgoingMessage = firstMessage;
                await TestDevice.ServiceClient.Messages.SendAsync(testDevice.Id, firstMessage, ct).ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"Sent 1st C2D message from service - to be received on callback: deviceId={testDevice.Id}, messageId={firstMessage.MessageId}");
                await testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);

                // Now unsubscribe from receiving c2d messages over the callback.
                await testDevice.DeviceClient.SetIncomingMessageCallbackAsync(null, ct).ConfigureAwait(false);

                // Send a message to the device from the service.
                OutgoingMessage secondMessage = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
                await TestDevice.ServiceClient.Messages.SendAsync(testDevice.Id, secondMessage, ct).ConfigureAwait(false);

                VerboseTestLogger.WriteLine($"Sent 2nd C2D message from service - should not be received on callback: deviceId={testDevice.Id}, messageId={secondMessage.MessageId}");
                Func<Task> receiveMessageOverCallback = async () =>
                {
                    await testDeviceCallbackHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
                };
                await receiveMessageOverCallback.Should().ThrowAsync<OperationCanceledException>();
            }

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    DevicePrefix,
                    transportSettings,
                    poolSize,
                    devicesCount,
                    InitOperationAsync,
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    authScope,
                    ct)
                .ConfigureAwait(false);
        }
    }
}
