// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Methods;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private const string MethodDevicePrefix = "MethodFaultInjectionPoolAmqpTests";
        private const string MethodName = "MethodE2EFaultInjectionPoolAmqpTests";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                        TestDeviceType.Sasl,
                        new AmqpTransportSettings(),
                        PoolingOverAmqp.SingleConnection_PoolSize,
                        PoolingOverAmqp.SingleConnection_DevicesCount,
                        MethodE2ETests.SetDeviceReceiveMethodAsync,
                        FaultInjection.FaultType_Tcp,
                        FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IotHubSak_DeviceMethodTcpConnRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodAmqpConnLostRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodAmqpConnLostRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodSessionLostRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHub_DeviceMethodSessionLostRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_IotHubSak_DeviceMethodTcpConnRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_Tcp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodAmqpConnLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpConn,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodSessionLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodSessionLostRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHub_DeviceMethodSessionLostRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpSess,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodReq,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod]
        public async Task Method_IoTHubSak_DeviceMethodRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_AmqpMethodResp,
                    FaultInjection.FaultCloseReason_Boom,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        [LoggedTestMethod]
        public async Task Method_DeviceSak_DeviceMethodGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondRecoveryPoolOverAmqpAsync(
                    TestDeviceType.Sasl,
                    new AmqpTransportSettings(TransportProtocol.WebSocket),
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    FaultInjection.FaultType_GracefulShutdownAmqp,
                    FaultInjection.FaultCloseReason_Bye)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecoveryPoolOverAmqpAsync(
            TestDeviceType type,
            ITransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            string faultType,
            string reason,
            TimeSpan delayInSec = default,
            TimeSpan durationInSec = default,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device,
            string proxyAddress = null)
        {
            async Task InitOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                Logger.Trace($"{nameof(MethodE2EPoolAmqpTests)}: Setting method callback handler for device {testDevice.Id}");
                await testDeviceCallbackHandler
                    .SetDeviceReceiveMethodAsync(MethodName, MethodE2ETests.DeviceResponseJson, MethodE2ETests.ServiceRequestJson)
                    .ConfigureAwait(false);
            }

            async Task TestOperationAsync(DeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

                Logger.Trace($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
                Task serviceSendTask = MethodE2ETests
                    .ServiceSendMethodAndVerifyResponseAsync(
                        testDevice.Id,
                        MethodName,
                        MethodE2ETests.DeviceResponseJson,
                        MethodE2ETests.ServiceRequestJson,
                        Logger);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);

                await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(List<DeviceClient> deviceClients, List<TestDeviceCallbackHandler> testDeviceCallbackHandlers)
            {
                deviceClients.ForEach(deviceClient => deviceClient.Dispose());
                testDeviceCallbackHandlers.ForEach(testDeviceCallbackHandler => testDeviceCallbackHandler.Dispose());

                await Task.FromResult<bool>(false).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    MethodDevicePrefix,
                    transportSettings,
                    proxyAddress,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    delayInSec == TimeSpan.Zero ? FaultInjection.DefaultFaultDelay : delayInSec,
                    durationInSec == TimeSpan.Zero ? FaultInjection.DefaultFaultDuration : durationInSec,
                    InitOperationAsync,
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    Logger)
                .ConfigureAwait(false);
        }
    }
}
