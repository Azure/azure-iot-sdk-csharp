// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("IoTHub-FaultInjection")]
    public partial class MessageSendFaultInjectionTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MessageSendFaultInjectionTests)}_";
        private readonly string ModulePrefix = $"E2E_{nameof(MessageSendFaultInjectionTests)}_";

        private readonly int MuxWithoutPoolingDevicesCount = 2;
        // For enabling multiplexing without pooling, the pool size needs to be set to 1
        private readonly int MuxWithoutPoolingPoolSize = 1;
        // These values are configurable
        private readonly int MuxWithPoolingDevicesCount = 4;
        private readonly int MuxWithPoolingPoolSize = 2;

        private static string ProxyServerAddress = Configuration.IoTHub.ProxyServerAddress;
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MessageSendFaultInjectionTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MuxedWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MuxedWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MuxedWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MuxedWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MuxedWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MuxedWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MuxedWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MuxedWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_Mqtt()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_TcpConnectionLossSendRecovery_MqttWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpConnectionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpConnectionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpConn,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpSessionLossSendRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpSessionLossSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpSess,
                "",
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpD2CLinkDropSendRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_AmqpD2CLinkDropSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_AmqpD2C,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_ThrottledConnectionRecovery_Amqp()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_ThrottledConnectionRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Amqp()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionLongTimeNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionLongTimeNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionLongTimeNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_ThrottledConnectionLongTimeNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.Device,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionLongTimeNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionLongTimeNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithoutPoolingPoolSize,
                    MuxWithoutPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionLongTimeNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_ThrottledConnectionLongTimeNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    MuxWithPoolingPoolSize,
                    MuxWithPoolingDevicesCount,
                    ConnectionStringAuthScope.IoTHub,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);

        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_ThrottledConnectionLongTimeNoRecovery_Http()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Http1,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        [TestMethod]
        // WIP: Exception handling
        public async Task Message_QuotaExceededNoRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        // WIP: Exception handling
        public async Task Message_QuotaExceededNoRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_QuotaExceededNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_QuotaExceededNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_QuotaExceededNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_QuotaExceededNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_QuotaExceededNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_QuotaExceededNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_QuotaExceededNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_QuotaExceededNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_QuotaExceeded,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // WIP: Exception handling
        [TestMethod]
        public async Task Message_QuotaExceededRecovery_Http()
        {
            await SendMessageRecovery(
                    TestDeviceType.Sasl,
                    Client.TransportType.Http1,
                    FaultInjection.FaultType_QuotaExceeded,
                    FaultInjection.FaultCloseReason_Boom,
                    FaultInjection.DefaultDelayInSec,
                    FaultInjection.DefaultDurationInSec,
                    FaultInjection.ShortRetryInMilliSec).ConfigureAwait(false);
        }

        [TestMethod]
        // WIP: Exception handling
        public async Task Message_AuthenticationErrorNoRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        // WIP: Exception handling
        public async Task Message_AuthenticationErrorNoRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_AuthenticationErrorNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_AuthenticationErrorNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_AuthenticationErrorNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_DeviceSak_AuthenticationErrorNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_AuthenticationErrorNoRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_AuthenticationErrorNoRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_AuthenticationErrorNoRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        // TODO: #839 - Disabling fault injection tests which expect an exception ( for multiplexed devices)
        [Ignore]
        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        // WIP: Exception handling
        public async Task Message_IoTHubSak_AuthenticationErrorNoRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        // WIP: Exception handling
        public async Task Message_AuthenticationErrorNoRecover_Http()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Http1,
                FaultInjection.FaultType_Auth,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec,
                FaultInjection.DefaultDurationInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownSendRecovery_Amqp()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownSendRecovery_AmqpWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.Device,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IotHubSak_GracefulShutdownSendRecovery_MuxWithoutPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_MuxWithoutPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithoutPoolingPoolSize,
                MuxWithoutPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_MuxWithPooling_Amqp()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("ConnectionPoolingE2ETests")]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_MuxWithPooling_AmqpWs()
        {
            await SendMessageRecoveryMuxedOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                MuxWithPoolingPoolSize,
                MuxWithPoolingDevicesCount,
                ConnectionStringAuthScope.IoTHub,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownSendRecovery_Mqtt()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_GracefulShutdownSendRecovery_MqttWs()
        {
            await SendMessageRecovery(
                TestDeviceType.Sasl,
                Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
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

        internal async Task SendMessageRecovery(
            TestDeviceType type,
            Client.TransportType transport,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec = FaultInjection.DefaultDurationInSec,
            int retryDurationInMilliSec = FaultInjection.RecoveryTimeMilliseconds)
        {
            

            Func<DeviceClient, TestDevice, Task> init = async (deviceClient, testDevice) =>
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                EventHubTestListener testListener = await EventHubTestListener.CreateListener(testDevice.Id).ConfigureAwait(false);
                string payload, p1Value;

                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = false;
                isReceived = await testListener.WaitForMessage(testDevice.Id, payload, p1Value).ConfigureAwait(false);
                Assert.IsTrue(isReceived);
                await testListener.CloseAsync().ConfigureAwait(false);
            };

            Func<Task> cleanupOperation = () =>
            {
                return Task.FromResult(false);
            };

            await FaultInjection.TestErrorInjectionSingleDeviceAsync(
                DevicePrefix,
                type,
                transport,
                faultType,
                reason,
                delayInSec,
                durationInSec,
                init,
                testOperation,
                cleanupOperation).ConfigureAwait(false);
        }

        internal async Task SendMessageRecoveryMuxedOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            ConnectionStringAuthScope authScope,
            string faultType,
            string reason,
            int delayInSec,
            int durationInSec = FaultInjection.DefaultDurationInSec,
            int retryDurationInMilliSec = FaultInjection.RecoveryTimeMilliseconds)
        {
            Func<DeviceClient, TestDevice, Task> init = async (deviceClient, testDevice) =>
            {
                deviceClient.OperationTimeoutInMilliseconds = (uint)retryDurationInMilliSec;
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                EventHubTestListener testListener = await EventHubTestListener.CreateListener(testDevice.Id).ConfigureAwait(false);
                string payload, p1Value;

                Client.Message testMessage = ComposeD2CTestMessage(out payload, out p1Value);
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = false;
                isReceived = await testListener.WaitForMessage(testDevice.Id, payload, p1Value).ConfigureAwait(false);
                Assert.IsTrue(isReceived);
                await testListener.CloseAsync().ConfigureAwait(false);
            };

            Func<Task> cleanupOperation = () =>
            {
                return Task.FromResult(false);
            };

            await FaultInjection.TestErrorInjectionMuxedOverAmqpAsync(
                DevicePrefix,
                authScope,
                type,
                transport,
                poolSize,
                devicesCount,
                faultType,
                reason,
                delayInSec,
                durationInSec,
                init,
                testOperation,
                cleanupOperation).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
