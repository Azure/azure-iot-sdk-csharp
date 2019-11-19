// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class MethodE2EPoolAmqpTests : IDisposable
    {
        private const string MethodName = "MethodE2EPoolAmqpTests";
        private readonly string DevicePrefix = $"E2E_{nameof(MethodE2EPoolAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MethodE2EPoolAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        // TODO: #943 - Honor different pool sizes for different connection pool settings.
        [Ignore]
        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_SingleConnection_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.SingleConnection_PoolSize,
                PoolingOverAmqp.SingleConnection_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_Amqp()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_Tcp_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MultipleConnections_AmqpWs()
        {
            await SendMethodAndRespondPoolOverAmqp(
                TestDeviceType.Sasl,
                Client.TransportType.Amqp_WebSocket_Only,
                PoolingOverAmqp.MultipleConnections_PoolSize,
                PoolingOverAmqp.MultipleConnections_DevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler,
                authScope: ConnectionStringAuthScope.IoTHub).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondPoolOverAmqp(
            TestDeviceType type,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            ConnectionStringAuthScope authScope = ConnectionStringAuthScope.Device)
        {
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Setting method for device {testDevice.Id}");
                Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MethodE2EPoolAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
                await MethodE2ETests.ServiceSendMethodAndVerifyResponse(
                    testDevice.Id, MethodName, MethodE2ETests.DeviceResponseJson, MethodE2ETests.ServiceRequestJson).ConfigureAwait(false);
            };

            Func<IList<DeviceClient>, Task> cleanupOperation = async (deviceClients) =>
            {
                foreach (DeviceClient deviceClient in deviceClients)
                {
                    deviceClient.Dispose();
                }
                await Task.FromResult<bool>(false).ConfigureAwait(false);
            };

            await PoolingOverAmqp.TestPoolAmqpAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation,
                authScope).ConfigureAwait(false);
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
