﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests : IDisposable
    {
        private const string MessageSend_DevicePrefix = "E2E_MessageSendFaultInjectionPoolAmqpTests";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IotHubSak_AmqpConnectionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_DeviceSak_AuthenticationNoRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_DeviceSak_AuthenticationNoRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_IoTHubSak_AuthenticationNoRecovery_SingleConnection_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_IoTHubSak_AuthenticationNoRecovery_SingleConnection_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IotHubSak_AmqpConnectionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_AmqpConnectionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_DeviceSak_AmqpD2CLinkDropSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        public async Task Message_IoTHubSak_AmqpD2CLinkDropSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpD2C,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_GracefulShutdownSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownSendRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_DeviceSak_ThrottledConnectionRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_IoTHubSak_ThrottledConnectionRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Throttle,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_DeviceSak_AuthenticationNoRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_DeviceSak_AuthenticationNoRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_IoTHubSak_AuthenticationNoRecovery_MultipleConnections_Amqp()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task Message_IoTHubSak_AuthenticationNoRecovery_MultipleConnections_AmqpWs()
        {
            await SendMessageRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Auth,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task SendMessageRecoveryPoolOverAmqpAsync(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            int delayInSec = FaultInjection.DefaultDelayInSec,
            int durationInSec = FaultInjection.DefaultDurationInSec,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}: Preparing to send message for device {testDevice.Id}");
                await deviceClient.OpenAsync().ConfigureAwait(false);

                (Client.Message testMessage, string payload, string p1Value) = MessageSendE2ETests.ComposeD2cTestMessage();

                _log.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}.{testDevice.Id}: payload='{payload}' p1Value='{p1Value}'");
                await deviceClient.SendEventAsync(testMessage).ConfigureAwait(false);

                bool isReceived = EventHubTestListener.VerifyIfMessageIsReceived(testDevice.Id, testMessage, payload, p1Value);
                Assert.IsTrue(isReceived, $"Message is not received for device {testDevice.Id}.");
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }

                return Task.FromResult(0);
            };

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MessageSend_DevicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    delayInSec,
                    durationInSec,
                    (d, t) => { return Task.FromResult(false); },
                    testOperation,
                    cleanupOperation,
                    authScope)
                .ConfigureAwait(false);
        }
    }
}
