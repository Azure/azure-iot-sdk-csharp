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

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "").ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceReportedPropertiesTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [Ignore]
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_SingleConnection_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTcpConnRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_Tcp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateGracefulShutdownRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_GracefulShutdownAmqp,
                FaultInjectionConstants.FaultCloseReason_Bye,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpConnectionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpConn,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateAmqpSessionLossRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpSess,
                "",
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinReqLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinReq,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_Amqp()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        // TODO: #950 - Link/session faults for message send/ method/ twin operations closes the connection.
        [TestMethod]
        [Timeout(LongRunningTestTimeoutMilliseconds)]
        public async Task Twin_DeviceSak_DeviceDesiredPropertyUpdateTwinRespLinkDropRecovery_MultipleConnections_AmqpWs()
        {
            await Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
                new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket),
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                FaultInjectionConstants.FaultType_AmqpTwinResp,
                FaultInjectionConstants.FaultCloseReason_Boom,
                TwinE2ETests.SetTwinPropertyUpdateCallbackHandlerAsync).ConfigureAwait(false);
        }

        private async Task Twin_DeviceReportedPropertiesRecoveryPoolOverAmqp(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            string proxyAddress = null)
        {
            async Task InitAsync(IotHubDeviceClient deviceClient, TestDevice t, TestDeviceCallbackHandler c)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler _)
            {
                VerboseTestLogger.WriteLine($"{nameof(TwinE2EPoolAmqpTests)}: Setting reported propery and verifying twin for device {testDevice.Id}");
                await TwinE2ETests.Twin_DeviceSetsReportedPropertyAndGetsItBackAsync(
                        deviceClient,
                        testDevice.Id,
                        Guid.NewGuid().ToString())
                    .ConfigureAwait(false);
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
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitAsync,
                    TestOperationAsync,
                    (d, c) => Task.FromResult(false),
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }

        private async Task Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp(
            IotHubClientTransportSettings transportSettings,
            int poolSize,
            int devicesCount,
            string faultType,
            string reason,
            Func<IotHubDeviceClient, string, string, Task<Task>> setTwinPropertyUpdateCallbackAsync,
            string proxyAddress = null)
        {
            var twinPropertyMap = new Dictionary<string, List<string>>();

            async Task InitAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                string propName = Guid.NewGuid().ToString();
                string propValue = Guid.NewGuid().ToString();
                twinPropertyMap.Add(testDevice.Id, new List<string> { propName, propValue });

                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}: Setting desired propery callback for device {testDevice.Id}");
                VerboseTestLogger.WriteLine($"{nameof(Twin_DeviceDesiredPropertyUpdateRecoveryPoolOverAmqp)}: name={propName}, value={propValue}");
                await testDeviceCallbackHandler.SetTwinPropertyUpdateCallbackHandlerAndProcessAsync(propName, propValue).ConfigureAwait(false);
            }

            async Task TestOperationAsync(IotHubDeviceClient deviceClient, TestDevice testDevice, TestDeviceCallbackHandler testDeviceCallbackHandler)
            {
                using var cts = new CancellationTokenSource(FaultInjection.RecoveryTime);

                List<string> twinProperties = twinPropertyMap[testDevice.Id];
                string propName = twinProperties[0];
                string propValue = twinProperties[1];

                VerboseTestLogger.WriteLine($"{nameof(FaultInjectionPoolAmqpTests)}: Updating the desired properties for device {testDevice.Id}");

                Task serviceSendTask = TwinE2ETests.RegistryManagerUpdateDesiredPropertyAsync(testDevice.Id, propName, propValue);
                Task twinReceivedTask = testDeviceCallbackHandler.WaitForTwinCallbackAsync(cts.Token);

                await Task.WhenAll(serviceSendTask, twinReceivedTask).ConfigureAwait(false);
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
                    FaultInjection.DefaultFaultDelay,
                    FaultInjection.DefaultFaultDuration,
                    InitAsync,
                    TestOperationAsync,
                    (d, c) => Task.FromResult(false),
                    ConnectionStringAuthScope.Device)
                .ConfigureAwait(false);
        }
    }
}
