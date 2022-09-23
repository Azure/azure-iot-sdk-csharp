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
    public partial class MessageSendFaultInjectionTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(MessageSendFaultInjectionTests)}_";
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        public async Task Message_TcpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Test device client recovery when proxy settings are enabled
        [TestCategory("Proxy")]
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs_WithProxy()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    proxyAddress: s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // Test device client recovery when proxy settings are enabled
        [TestCategory("Proxy")]
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs_WithProxy()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    proxyAddress: s_proxyServerAddress)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_Throttle,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("Flaky")]
        public async Task Message_ThrottledConnectionRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Throttle,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Amqp()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(),
                        FaultInjectionConstants.FaultType_Throttle,
                        FaultInjectionConstants.FaultCloseReason_Boom,
                        FaultInjection.ShortRetryDuration)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.Throttled);
            error.And.IsTransient.Should().BeTrue();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                        FaultInjectionConstants.FaultType_Throttle,
                        FaultInjectionConstants.FaultCloseReason_Boom,
                        FaultInjection.ShortRetryDuration)
                    .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.Throttled);
            error.And.IsTransient.Should().BeTrue();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize]
        public async Task Message_QuotaExceededRecovery_Amqp()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(),
                        FaultInjectionConstants.FaultType_QuotaExceeded,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.DeviceMaximumQueueDepthExceeded);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        [DoNotParallelize]
        public async Task Message_QuotaExceededRecovery_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                        FaultInjectionConstants.FaultType_QuotaExceeded,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.DeviceMaximumQueueDepthExceeded);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AuthenticationRecovery_Amqp()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(),
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AuthenticationRecovery_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [TestCategory("FaultInjectionBVT")]
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // Graceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [TestCategory("FaultInjectionBVT")]
        public async Task Message_GracefulShutdownSendRecovery_Mqtt()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        internal async Task SendMessageRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            TimeSpan retryDuration = default,
            string proxyAddress = null)
        {
            TimeSpan operationTimeout = retryDuration == TimeSpan.Zero
                ? FaultInjection.RecoveryTime
                : retryDuration;

            async Task InitAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                using var cts = new CancellationTokenSource(retryDuration);
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice)
            {
                Client.Message testMessage = MessageSendE2ETests.ComposeD2cTestMessage(Logger, out string _, out string _);
                using var cts = new CancellationTokenSource(operationTimeout);
                await deviceClient.SendEventAsync(testMessage, cts.Token).ConfigureAwait(false);
            };

            await FaultInjection
                .TestErrorInjectionAsync(
                    _devicePrefix,
                    TestDeviceType.Sasl,
                    transportSettings,
                    proxyAddress,
                    faultType,
                    reason,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitAsync,
                    TestOperationAsync,
                    () => Task.FromResult(false),
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
