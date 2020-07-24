﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.Azure.Devices.E2ETests.Helpers.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class MethodE2EPoolAmqpTests : E2EMsTestBase
    {
        private const string MethodName = "MethodE2EPoolAmqpTests";
        private readonly string _devicePrefix = $"E2E_{nameof(MethodE2EPoolAmqpTests)}_";
        private static readonly TestLogger s_log = TestLogger.GetInstance();

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.SingleConnection_PoolSize,
                    PoolingOverAmqp.SingleConnection_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_Tcp_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                    Client.TransportType.Amqp_WebSocket_Only,
                    PoolingOverAmqp.MultipleConnections_PoolSize,
                    PoolingOverAmqp.MultipleConnections_DevicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync,
                    authScope: ConnectionStringAuthScope.IoTHub)
                .ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondPoolOverAmqp(
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                s_log.Trace($"{nameof(MethodE2EPoolAmqpTests)}: Setting method for device {testDevice.Id}");
                Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                s_log.Trace($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
                await MethodE2ETests
                    .ServiceSendMethodAndVerifyResponseAsync(
                        testDevice.Id,
                        MethodName,
                        MethodE2ETests.DeviceResponseJson,
                        MethodE2ETests.ServiceRequestJson)
                    .ConfigureAwait(false);
            };

            await PoolingOverAmqp
                .TestPoolAmqpAsync(
                    _devicePrefix,
                    transport,
                    poolSize,
                    devicesCount,
                    initOperation,
                    testOperation,
                    null,
                    authScope,
                    true)
                .ConfigureAwait(false);
        }
    }
}
