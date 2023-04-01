// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private const string MessageSend_DevicePrefix = "MessageSendFaultInjectionPoolAmqpTests";

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Tcp, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_GracefulShutdownAmqp, FaultInjectionConstants.FaultCloseReason_Bye)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_AmqpConn,"")]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_AmqpConn, "")]
        public async Task Telemetry_ConnectionLossRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    faultType,
                    faultReason,
                    ct: ct)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AmqpSessionLossSendRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "",
                    ct: ct)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AmqpD2cLinkDropSendRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct: ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_ThrottledConnectionRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(protocol),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Throttle,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct: ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [TestCategory("LongRunning")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AuthenticationNoRecovery_MultipleConnections_Amqp(IotHubClientTransportProtocol protocol)
        {
            // arrange
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryPoolOverAmqpAsync(
                        new IotHubClientAmqpSettings(protocol),
                        PoolingOverAmqp.MultipleConnections_PoolSize,
                        PoolingOverAmqp.MultipleConnections_DevicesCount,
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom,
                        ct: ct)
                    .ConfigureAwait(false);
            };

            // assert
            ExceptionAssertions<IotHubClientException> error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        // Test device client recovery when proxy settings are enabled
        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Telemetry_ConnectionLossRecovery_MultipleConnections_AmqpWs_WithProxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    s_proxyServerAddress,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task SendMessageRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            string proxyAddress = null,
            CancellationToken ct = default)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _, CancellationToken ct)
            {
                TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}.{testDevice.Id}: payload='{payload}' p1Value='{p1Value}'");
                await testDevice.DeviceClient.SendTelemetryAsync(testMessage, ct).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MessageSend_DevicePrefix,
                    transportSettings,
                    proxyAddress,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    (t, c, ct) => Task.FromResult(false),
                    TestOperationAsync,
                    (t, c, ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
