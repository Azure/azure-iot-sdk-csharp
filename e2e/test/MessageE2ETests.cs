// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public partial class MessageE2ETests : IDisposable
    {
        private const string DevicePrefix = "E2E_Message_";
        private const string ModulePrefix = "E2E_Module_";
        private const string DevicePrefixTimeout = "E2E_Message_Timeout_";
        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageE2ETests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Amqp()
        {
            await SendSingleMessage(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_AmqpWs()
        {
            await SendSingleMessage(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Mqtt()
        {
            await SendSingleMessage(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_MqttWs()
        {
            await SendSingleMessage(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSendSingleMessage_Http()
        {
            await SendSingleMessage(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Amqp()
        {
            await ReceiveSingleMessage(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_AmqpWs()
        {
            await ReceiveSingleMessage(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Mqtt()
        {
            await ReceiveSingleMessage(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_MqttWs()
        {
            await ReceiveSingleMessage(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceReceiveSingleMessage_Http()
        {
            await ReceiveSingleMessage(Client.TransportType.Http1).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_Http_WithProxy()
        {
            Client.Http1TransportSettings httpTransportSettings = new Client.Http1TransportSettings();
            httpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_Http_WithCustomeProxy()
        {
            Http1TransportSettings httpTransportSettings = new Http1TransportSettings();
            CustomWebProxy proxy = new CustomWebProxy();
            httpTransportSettings.Proxy = proxy;
            ITransportSettings[] transportSettings = new ITransportSettings[] { httpTransportSettings };

            await SendSingleMessage(transportSettings).ConfigureAwait(false);
            Assert.AreNotEqual(proxy.Counter, 0);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_AmqpWs_WithProxy()
        {
            Client.AmqpTransportSettings amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessage(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_DeviceSendSingleMessage_MqttWs_WithProxy()
        {
            Client.Transport.Mqtt.MqttTransportSettings mqttTransportSettings = 
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessage(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_ModuleSendSingleMessage_AmqpWs_WithProxy()
        {
            Client.AmqpTransportSettings amqpTransportSettings = new Client.AmqpTransportSettings(Client.TransportType.Amqp_WebSocket_Only);
            amqpTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { amqpTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ProxyE2ETests")]
        public async Task Message_ModuleSendSingleMessage_MqttWs_WithProxy()
        {
            Client.Transport.Mqtt.MqttTransportSettings mqttTransportSettings = 
                new Client.Transport.Mqtt.MqttTransportSettings(Client.TransportType.Mqtt_WebSocket_Only);
            mqttTransportSettings.Proxy = new WebProxy(ProxyServerAddress);
            ITransportSettings[] transportSettings = new ITransportSettings[] { mqttTransportSettings };

            await SendSingleMessageModule(transportSettings).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpD2CLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_TcpConnectionLossReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #239.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpConnectionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpConn, "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpSessionLossReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpSessionLossReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpC2D,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_AmqpC2DLinkDropReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO #588 - Fails with InternalServerError intermittently.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Throttle,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO #588 - Fails with InternalServerError intermittently.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_ThrottledConnectionRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Throttle,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO #588 - Fails with InternalServerError intermittently.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Throttle,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec,
                FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        [Ignore] // TODO #588 - Fails with InternalServerError intermittently.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Throttle,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec,
                FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        [Ignore] // TODO #588 - Fails with InternalServerError intermittently.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(System.TimeoutException))]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Http()
        {
            await SendMessageThrottledForHttp().ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.DeviceMaximumQueueDepthExceededException))]
        public async Task Message_QuotaExceededRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.DeviceMaximumQueueDepthExceededException))]
        public async Task Message_QuotaExceededRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.QuotaExceededException))]
        public async Task Message_QuotaExceededRecovery_Http()
        {
            await SendMessageRecovery(Client.TransportType.Http1,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        [ExpectedException(typeof(Client.Exceptions.UnauthorizedException))]
        public async Task Message_AuthenticationRecovery_Http()
        {
            await SendMessageRecovery(Client.TransportType.Http1,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_SendGracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownSendRecovery_Mqtt()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownReceiveRecovery_Amqp()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #239
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownReceiveRecovery_AmqpWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownReceiveRecovery_Mqtt()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Message_GracefulShutdownReceiveRecovery_MqttWs()
        {
            await ReceiveMessageRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefixTimeout).ConfigureAwait(false);
            ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Amqp);
            await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), null).ConfigureAwait(false);
        }

        private async Task FastTimeout()
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefixTimeout).ConfigureAwait(false);
            ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, Client.TransportType.Amqp);
            await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), TimeSpan.FromTicks(1)).ConfigureAwait(false);
        }

        private Client.Message ComposeD2CTestMessage(out string payload, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeD2CTestMessage)}: payload='{payload}' p1Value='{p1Value}'");

            return new Client.Message(Encoding.UTF8.GetBytes(payload))
            {
                Properties = { ["property1"] = p1Value }
            };
        }

        private Message ComposeC2DTestMessage(out string payload, out string messageId, out string p1Value)
        {
            payload = Guid.NewGuid().ToString();
            messageId = Guid.NewGuid().ToString();
            p1Value = Guid.NewGuid().ToString();

            _log.WriteLine($"{nameof(ComposeC2DTestMessage)}: payload='{payload}' messageId='{messageId}' p1Value='{p1Value}'");

            return new Message(Encoding.UTF8.GetBytes(payload))
            {
                MessageId = messageId,
                Properties = { ["property1"] = p1Value }
            };
        }

        private async Task VerifyReceivedC2DMessage(Client.TransportType transport, DeviceClient dc, string payload, string p1Value)
        {
            var wait = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (wait)
            {
                Client.Message receivedMessage;

                if (transport == Client.TransportType.Http1)
                {
                    // Long-polling is not supported in http
                    receivedMessage = await dc.ReceiveAsync().ConfigureAwait(false);
                }
                else
                {
                    receivedMessage = await dc.ReceiveAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }

                if (receivedMessage != null)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Assert.AreEqual(messageData, payload);

                    Assert.AreEqual(receivedMessage.Properties.Count, 1);
                    var prop = receivedMessage.Properties.Single();
                    Assert.AreEqual(prop.Key, "property1");
                    Assert.AreEqual(prop.Value, p1Value);

                    await dc.CompleteAsync(receivedMessage).ConfigureAwait(false);
                    wait = false;
                }

                if (sw.Elapsed.TotalSeconds > 5)
                {
                    throw new TimeoutException("Test is running longer than expected.");
                }
            }

            sw.Stop();
        }

        internal async Task SendSingleMessage(Client.TransportType transport)
        {
            ITransportSettings[] transportSettings;
            switch (transport)
            {
                case Client.TransportType.Amqp_Tcp_Only:
                case Client.TransportType.Amqp_WebSocket_Only:
                    transportSettings = new ITransportSettings[] { new Client.AmqpTransportSettings(transport) };
                    break;

                case Client.TransportType.Mqtt_Tcp_Only:
                case Client.TransportType.Mqtt_WebSocket_Only:
                    transportSettings = new ITransportSettings[] { new Client.Transport.Mqtt.MqttTransportSettings(transport) };
                    break;

                case Client.TransportType.Http1:
                    transportSettings = new ITransportSettings[] { new Client.Http1TransportSettings() };
                    break;

                default:
                    throw new NotSupportedException($"Unknown transport: '{transport}'.");
            }

            await SendSingleMessage(transportSettings).ConfigureAwait(false);
        }

        internal async Task SendSingleMessageModule(ITransportSettings[] transportSettings)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(DevicePrefix, ModulePrefix).ConfigureAwait(false);
            var moduleClient = ModuleClient.CreateFromConnectionString(testModule.ConnectionString, transportSettings);

            try
            {
                await moduleClient.OpenAsync().ConfigureAwait(false);

                string payload;
                string p1Value;
                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await moduleClient.SendEventAsync(testMessage).ConfigureAwait(false);
            }
            finally
            {
                await moduleClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveSingleMessage(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            try
            {

                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    _log.WriteLine("Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.");
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }

                string payload, messageId, p1Value;
                await serviceClient.OpenAsync().ConfigureAwait(false);

                Message msg = ComposeC2DTestMessage(out payload, out messageId, out p1Value);
                await serviceClient.SendAsync(testDevice.Id, msg).ConfigureAwait(false);

                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task ReceiveMessageRecovery(Client.TransportType transport, string faultType, string reason,
            int delayInSec)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                if (transport == Client.TransportType.Mqtt_Tcp_Only ||
                    transport == Client.TransportType.Mqtt_WebSocket_Only)
                {
                    // Dummy ReceiveAsync to ensure mqtt subscription registration before SendAsync() is called on service client.
                    await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                }

                string payload, messageId, p1Value;
                await serviceClient.OpenAsync().ConfigureAwait(false);
                await serviceClient.SendAsync(
                    testDevice.Id,
                    ComposeC2DTestMessage(out payload, out messageId, out p1Value)).ConfigureAwait(false);
                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value).ConfigureAwait(false);

                // send error command
                await deviceClient.SendEventAsync(
                    FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec)).ConfigureAwait(false);

                await Task.Delay(FaultInjection.WaitForDisconnectMilliseconds).ConfigureAwait(false);
                await serviceClient.SendAsync(
                    testDevice.Id,
                    ComposeC2DTestMessage(out payload, out messageId, out p1Value)).ConfigureAwait(false);
                await VerifyReceivedC2DMessage(transport, deviceClient, payload, p1Value).ConfigureAwait(false);
            }
            finally
            {
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await serviceClient.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}

