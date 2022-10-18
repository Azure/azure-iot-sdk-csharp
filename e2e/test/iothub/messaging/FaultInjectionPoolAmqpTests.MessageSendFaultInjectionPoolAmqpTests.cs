// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private const string MessageSend_DevicePrefix = "MessageSendFaultInjectionPoolAmqpTests";

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [TestCategory("LongRunning")]
        public async Task Message_GracefulShutdownSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_ThrottledConnectionRecovery_MultipleConnections_Amqp()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryPoolOverAmqpAsync(
                        new IotHubClientAmqpSettings(),
                        PoolingOverAmqp.MultipleConnections_PoolSize,
                        PoolingOverAmqp.MultipleConnections_DevicesCount,
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_ThrottledConnectionRecovery_MultipleConnections_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryPoolOverAmqpAsync(
                        new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                        PoolingOverAmqp.MultipleConnections_PoolSize,
                        PoolingOverAmqp.MultipleConnections_DevicesCount,
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AuthenticationNoRecovery_MultipleConnections_Amqp()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryPoolOverAmqpAsync(
                        new IotHubClientAmqpSettings(),
                        PoolingOverAmqp.MultipleConnections_PoolSize,
                        PoolingOverAmqp.MultipleConnections_DevicesCount,
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_AuthenticationNoRecovery_MultipleConnections_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryPoolOverAmqpAsync(
                        new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                        PoolingOverAmqp.MultipleConnections_PoolSize,
                        PoolingOverAmqp.MultipleConnections_DevicesCount,
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        // Test device client recovery when proxy settings are enabled
        [TestCategory("Proxy")]
        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MultipleConnections_AmqpWs_WithProxy()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        private async Task SendMessageRecoveryPoolOverAmqpAsync(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            string proxyAddress = null)
        {
            async Task TestInitAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler callbackHandler)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                TelemetryMessage testMessage = MessageSendE2ETests.ComposeD2cTestMessage(Logger, out string payload, out string p1Value);

                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}.{testDevice.Id}: payload='{payload}' p1Value='{p1Value}'");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.SendTelemetryAsync(testMessage, cts.Token).ConfigureAwait(false);
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
                    TestInitAsync,
                    TestOperationAsync,
                    (d, c) => Task.FromResult(false),
                    ConnectionStringAuthScope.Device,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
