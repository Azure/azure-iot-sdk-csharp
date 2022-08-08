// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.Azure.Devices.E2ETests.Twins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    public partial class FaultInjectionPoolAmqpTests
    {
        private readonly string Twin_DevicePrefix = $"TwinFaultInjectionPoolAmqpTests";

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTcpConnRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpSessionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpSessionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinReq,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(),
                1,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [LoggedTestMethod, Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_IoTHubSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                TestDeviceType.Sasl,
                new IotHubClientAmqpSettings(TransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjection.FaultType_AmqpTwinResp,
                FaultInjection.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
            TestDeviceType type,
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            TimeSpan delayInSec = default,
            TimeSpan durationInSec = default,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device,
            string proxyAddress = null)
        {
            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                Logger.Trace($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(deviceClient, testDevice.Id, Guid.NewGuid().ToString(), Logger).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(List<IotHubDeviceClient> deviceClients, List<TestDeviceCallbackHandler> _)
            {
                deviceClients.ForEach(deviceClient => deviceClient.Dispose());
                await Task.FromResult<bool>(false).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    Twin_DevicePrefix,
                    transportSettings,
                    proxyAddress,
                    poolSize,
                    devicesCount,
                    faultType,
                    reason,
                    delayInSec == TimeSpan.Zero ? FaultInjection.DefaultFaultDelay : delayInSec,
                    durationInSec == TimeSpan.Zero ? FaultInjection.DefaultFaultDuration : durationInSec,
                    (d, t, h) => { return Task.FromResult(false); },
                    TestOperationAsync,
                    CleanupOperationAsync,
                    authScope,
                    Logger)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
            TestDeviceType type,
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            Func<IotHubDeviceClient, string, string, MsTestLogger, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            TimeSpan delayInSec = default,
            TimeSpan durationInSec = default,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device,
            string proxyAddress = null)
        {
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                Logger.Trace($"{nameof(Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp)}: name={propName}, value={propValue}");
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAsync(propName).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                string propName = twinProperties[0];
                string propValue = twinProperties[1];
                testDeviceCallbackHandler.ExpectedTwinPropertyValue = propValue;

                Logger.Trace($"{nameof(FaultInjectionPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");

                Task serviceSendTask = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(cts.Token);

                await Task.WhenAll(serviceSendTask, twinReceivedTask).ConfigureAwait(false);
            }

            async Task CleanupOperationAsync(List<IotHubDeviceClient> deviceClients, List<TestDeviceCallbackHandler> testDeviceCallbackHandlers)
            {
                deviceClients.ForEach(deviceClient => deviceClient.Dispose());
                testDeviceCallbackHandlers.ForEach(testDeviceCallbackHandler => testDeviceCallbackHandler.Dispose());

                twinPropertyMap.Clear();
                await Task.FromResult<bool>(false).ConfigureAwait(false);
            }

            await FaultInjectionPoolingOverAmqp
                .TestFaultInjectionPoolAmqpAsync(
                    Twin_DevicePrefix,
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
