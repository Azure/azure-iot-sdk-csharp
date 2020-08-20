﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private readonly string MessageReceive_DevicePrefix = $"MessageReceiveFaultInjectionPoolAmqpTests";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IotHubSak_AmqpConnectionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpConnectionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpC2DLinkDropReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpC2DLinkDropReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpC2DLinkDropReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpC2DLinkDropReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Message_DeviceSak_GracefulShutdownReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_GracefulShutdownReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownReceiveRecovery_SingleConnection_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownReceiveRecovery_SingleConnection_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_TcpConnectionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_TcpConnectionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpConnectionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "")
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IotHubSak_AmqpConnectionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpConn,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpConnectionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
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
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpSessionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "")
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpSessionLossReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpSess,
                    "",
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [TestCategory("LongRunning")]
        public async Task Message_DeviceSak_AmqpC2DLinkDropReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_AmqpC2DLinkDropReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpC2DLinkDropReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_AmqpC2DLinkDropReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_AmqpC2D,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_GracefulShutdownReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_DeviceSak_GracefulShutdownReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownReceiveRecovery_MultipleConnections_Amqp()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Message_IoTHubSak_GracefulShutdownReceiveRecovery_MultipleConnections_AmqpWs()
        {
            await ReceiveMessageRecoveryPoolOverAmqp(
                    TestDeviceType.Sasl,
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task ReceiveMessageRecoveryPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            int delayInSec = FaultInjection.DefaultDelayInSec,
            int durationInSec = FaultInjection.DefaultDurationInSec,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device,
            string proxyAddress = null)
        {
            // Initialize the service client
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                (Message msg, string payload, string p1Value) = MessageReceiveE2ETests.ComposeC2dTestMessage(Logger);
                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}: Sending message to device {testDevice.Id}: payload='{payload}' p1Value='{p1Value}'");
                await serviceClient.SendAsync(testDevice.Id, msg)
                .ConfigureAwait(false);

                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}: Preparing to receive message for device {testDevice.Id}");
                await deviceClient.OpenAsync()
                .ConfigureAwait(false);
                await MessageReceiveE2ETests.VerifyReceivedC2DMessageAsync(transport, deviceClient, testDevice.Id, msg, payload, Logger)
                .ConfigureAwait(false);
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                await serviceClient.CloseAsync()
                .ConfigureAwait(false);
                serviceClient.Dispose();

                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
            };

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MessageReceive_DevicePrefix,
                    transport,
                    proxyAddress,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    delayInSec,
                    durationInSec,
                    (d, t) => { return Task.FromResult(false); },
                    testOperation,
                    cleanupOperation,
                    authScope,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
