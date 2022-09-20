// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
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
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                    Client.TransportType.Mqtt_Tcp_Only,
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Mqtt_WebSocket_Only,
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
                    Client.TransportType.Mqtt_WebSocket_Only,
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
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_AmqpD2cLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    Client.TransportType.Amqp_Tcp_Only,
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
                    Client.TransportType.Amqp_WebSocket_Only,
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
                        Client.TransportType.Amqp_Tcp_Only,
                        FaultInjectionConstants.FaultType_Throttle,
                        FaultInjectionConstants.FaultCloseReason_Boom,
                        FaultInjection.ShortRetryDuration)
                    .ConfigureAwait(false);

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
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    FaultInjection.ShortRetryDuration).ConfigureAwait(false);
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
                    FaultInjection.FaultType_QuotaExceeded,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.StatusCode.Should().Be(IotHubStatusCode.DeviceMaximumQueueDepthExceeded);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DoNotParallelize]
        public async Task Message_QuotaExceededRecovery_AmqpWs()
        {
            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_QuotaExceeded,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration)
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
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration).ConfigureAwait(false);
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
                    TestDeviceType.Sasl,
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration)
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
        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
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
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjection.FaultType_GracefulShutdownMqtt,
                    FaultInjection.FaultCloseReason_Bye,
                    FaultInjection.DefaultFaultDelay)
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
                // Some tests rely on the operation timing out before fault injection ends, so this is important to set.
                // But it could probably just as easily be done with cancellation tokens.
                deviceClient.OperationTimeoutInMilliseconds = (uint)operationTimeout.TotalMilliseconds;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await deviceClient.OpenAsync(cts.Token).ConfigureAwait(false);
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDuration.TotalMilliseconds;
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice)
            {
                (Client.Message testMessage, string payload, string p1Value) = MessageSendE2ETests.ComposeD2cTestMessage(Logger);
                using var cts = new CancellationTokenSource(operationTimeoutInMilliSecs);
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
