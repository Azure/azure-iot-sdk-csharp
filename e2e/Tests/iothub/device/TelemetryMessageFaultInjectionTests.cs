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
    [TestCategory("FaultInjection")]
    [TestCategory("IoTHub-Client")]
    public partial class TelemetryMessageFaultInjectionTests : E2EMsTestBase
    {
        private readonly string _devicePrefix = $"{nameof(TelemetryMessageFaultInjectionTests)}_";
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;

        // Ungraceful disconnection recovery test is marked as a build verification test
        // to test client reconnection logic in PR runs.
        [DataTestMethod]
        [TestCategory("FaultInjectionBVT")]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_ConnectionLossRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
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
        public async Task Telemetry_GracefulShutdownRecovery_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
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
        public async Task Telemetry_ConnectionLossRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
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
        public async Task Telemetry_GracefulShutdownRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_GracefulShutdownMqtt,
                    FaultInjectionConstants.FaultCloseReason_Bye,
                    ct)
                .ConfigureAwait(false);
        }

        // Test device client recovery when proxy settings are enabled
        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Telemetry_ConnectionLossSendRecovery_MqttWs_WithProxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    s_proxyServerAddress,
                    ct)
                .ConfigureAwait(false);
        }

        // Test device client recovery when proxy settings are enabled
        [TestMethod]
        [TestCategory("Proxy")]
        public async Task Telemetry_ConnectionLossSendRecovery_AmqpWs_WithProxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                    FaultInjectionConstants.FaultType_Tcp,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    s_proxyServerAddress,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AmqpConnectionLossSendRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpConn,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AmqpSessionLossSendRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpSess,
                    "",
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AmqpD2cLinkDropSendRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    FaultInjectionConstants.FaultType_AmqpD2C,
                    FaultInjectionConstants.FaultCloseReason_Boom,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DoNotParallelize]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_Throttle, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_Throttle, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.Tcp, FaultInjectionConstants.FaultType_QuotaExceeded, FaultInjectionConstants.FaultCloseReason_Boom)]
        [DataRow(IotHubClientTransportProtocol.WebSocket, FaultInjectionConstants.FaultType_QuotaExceeded, FaultInjectionConstants.FaultCloseReason_Boom)]
        public async Task Telemetry_ServicLevelRecoverableFault_Amqp(IotHubClientTransportProtocol protocol, string faultType, string faultReason)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMessageRecoveryAsync(
                    new IotHubClientAmqpSettings(protocol),
                    faultType,
                    faultReason,
                    ct)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Telemetry_AuthenticationErrorNoRecovery_Amqp(IotHubClientTransportProtocol protocol)
        {
            // arrange
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            // act
            Func<Task> act = async () =>
            {
                await SendMessageRecoveryAsync(
                        new IotHubClientAmqpSettings(protocol),
                        FaultInjectionConstants.FaultType_Auth,
                        FaultInjectionConstants.FaultCloseReason_Boom,
                        ct)
                .ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.Unauthorized);
            error.And.IsTransient.Should().BeFalse();
        }

        internal async Task SendMessageRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            CancellationToken ct)
        {
            await SendMessageRecoveryAsync(transportSettings, faultType, reason, null, ct).ConfigureAwait(false);
        }

        internal async Task SendMessageRecoveryAsync(
            IotHubClientTransportSettings transportSettings,
            string faultType,
            string reason,
            string proxyAddress,
            CancellationToken ct)
        {
            async Task TestOperationAsync(TestDevice testDevice, TestDeviceCallbackHandler _, CancellationToken ct)
            {
                TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
                await testDevice.DeviceClient.SendTelemetryAsync(testMessage, ct).ConfigureAwait(false);
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
                    (d, c, ct) => Task.FromResult(false),
                    TestOperationAsync,
                    (ct) => Task.FromResult(false),
                    ct)
                .ConfigureAwait(false);
        }
    }
}
