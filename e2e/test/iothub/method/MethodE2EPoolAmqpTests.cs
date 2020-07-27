// Copyright (c) Microsoft. All rights reserved.
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

        [DataTestMethod]
        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        //[DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        public async Task Method_DeviceReceivesMethodAndResponse(Client.TransportType transportType, ConnectionStringAuthScope authScope, int poolSize, int devicesCount)
        {
            await SendMethodAndRespondPoolOverAmqp(
                    transportType,
                    poolSize,
                    devicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodAsync,
                    authScope)
                .ConfigureAwait(false);
        }

        [DataTestMethod]
        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        //[DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        //[DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.SingleConnection_PoolSize, PoolingOverAmqp.SingleConnection_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.Device, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_Tcp_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        [DataRow(Client.TransportType.Amqp_WebSocket_Only, ConnectionStringAuthScope.IoTHub, PoolingOverAmqp.MultipleConnections_PoolSize, PoolingOverAmqp.MultipleConnections_DevicesCount)]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultHandler(Client.TransportType transportType, ConnectionStringAuthScope authScope, int poolSize, int devicesCount)
        {
            await SendMethodAndRespondPoolOverAmqp(
                    transportType,
                    poolSize,
                    devicesCount,
                    MethodE2ETests.SetDeviceReceiveMethodDefaultHandlerAsync,
                    authScope)
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
