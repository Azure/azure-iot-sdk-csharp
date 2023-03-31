// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Messaging
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public partial class TelemetryMessageE2eTests : E2EMsTestBase
    {
        private const int MessageBatchCount = 5;

        // The maximum message size for device to cloud messages is 256 KB. We are allowing 1 KB of buffer for message header information etc.
        private const int LargeMessageSizeInBytes = 255 * 1024;

        // The size of a device to cloud message. This overly exceeds the maximum message size set by the hub, which is 256 KB.
        // The reason why we are testing for this case is because we noticed a different behavior between this case, and the case where
        // the message size is less than 1 MB.
        private const int OverlyExceedAllowedMessageSizeInBytes = 3000 * 1024;

        private readonly string _idPrefix = $"{nameof(TelemetryMessageE2eTests)}_";
        private static readonly string s_proxyServerAddress = TestConfiguration.IotHub.ProxyServerAddress;

        [TestMethod]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Mqtt(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
            await SendSingleMessageAsync(testDeviceType, new IotHubClientMqttSettings(protocol), testMessage, ct).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_MqttWs_WithProxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            var mqttTransportSettings = new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)
            {
                Proxy = new WebProxy(s_proxyServerAddress),
            };
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);

            await SendSingleMessageAsync(TestDeviceType.X509, mqttTransportSettings, testMessage, ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleMessage_Amqp_WithHeartbeats(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var amqpTransportSettings = new IotHubClientAmqpSettings(protocol)
            {
                IdleTimeout = TimeSpan.FromMinutes(2),
            };
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);

            await SendSingleMessageAsync(testDeviceType, amqpTransportSettings, testMessage, ct).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("Proxy")]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            var amqpTransportSettings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)
            {
                Proxy = new WebProxy(s_proxyServerAddress),
            };
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);

            await SendSingleMessageAsync(TestDeviceType.X509, amqpTransportSettings, testMessage, ct).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendBatchMessages_Amqp(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_longRunningTestTimeout);
            CancellationToken ct = cts.Token;

            await SendBatchMessagesAsync(testDeviceType, new IotHubClientAmqpSettings(protocol), ct).ConfigureAwait(false);
        }

        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleLargeMessageAsync_Amqp(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var transportSettings = new IotHubClientAmqpSettings(protocol);
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessageOfSpecifiedSize(LargeMessageSizeInBytes, out string _, out string _);

            await SendSingleMessageAsync(testDeviceType, transportSettings, testMessage, ct);
        }

        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleLargeMessageAsync_Mqtt(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var transportSettings = new IotHubClientMqttSettings(protocol);
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessageOfSpecifiedSize(LargeMessageSizeInBytes, out string _, out string _);

            await SendSingleMessageAsync(testDeviceType, transportSettings, testMessage, ct);
        }

        // We cannot test this over MQTT since MQTT will disconnect the client if it receives an invalid payload.
        [DataTestMethod]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.Sasl, IotHubClientTransportProtocol.WebSocket)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.Tcp)]
        [DataRow(TestDeviceType.X509, IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceSendSingleOverlyLargeMessageAsync_Amqp(TestDeviceType testDeviceType, IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var transportSettings = new IotHubClientAmqpSettings(protocol);
            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessageOfSpecifiedSize(OverlyExceedAllowedMessageSizeInBytes, out string _, out string _);

            Func<Task> actionAsync = async () => await SendSingleMessageAsync(testDeviceType, transportSettings, testMessage, ct);
            await actionAsync
                .Should()
                .ThrowAsync<IotHubClientException>()
                .Where(ex => ex.ErrorCode == IotHubClientErrorCode.MessageTooLarge);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceOpenCloseOpenSendSingleMessage_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
            await OpenCloseOpenThenSendSingleMessage(TestDeviceType.Sasl, new IotHubClientAmqpSettings(protocol), testMessage, ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Message_DeviceOpenCloseOpenSendSingleMessage_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string _, out string _);
            await OpenCloseOpenThenSendSingleMessage(TestDeviceType.Sasl, new IotHubClientMqttSettings(protocol), testMessage, ct).ConfigureAwait(false);
        }

        private async Task SendSingleMessageAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings, TelemetryMessage testMessage, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix, type, ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            await deviceClient.SendTelemetryAsync(testMessage, ct).ConfigureAwait(false);
        }

        private async Task SendBatchMessagesAsync(TestDeviceType type, IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix, type, ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            var messagesToBeSent = new Dictionary<TelemetryMessage, Tuple<string, string>>();

            for (int i = 0; i < MessageBatchCount; i++)
            {
                TelemetryMessage testMessage = TelemetryMessageHelper.ComposeTestMessage(out string payload, out string p1Value);
                messagesToBeSent.Add(testMessage, Tuple.Create(payload, p1Value));
            }
            await deviceClient.SendTelemetryAsync(messagesToBeSent.Keys.ToList(), ct).ConfigureAwait(false);
        }

        private async Task OpenCloseOpenThenSendSingleMessage(TestDeviceType type, IotHubClientTransportSettings transportSettings, TelemetryMessage testMessage, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_idPrefix, type, ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            // Close and re-open the client under test.
            await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            // The re-opened client under test should still be able to send telemetry.
            await deviceClient.SendTelemetryAsync(testMessage, ct).ConfigureAwait(false);
        }
    }
}
