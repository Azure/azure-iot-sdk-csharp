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
    public class MethodE2EMultiplexingOverAmqpTests : IDisposable
    {
        private const string MethodName = "MethodE2EMultiplexingOverAmqpTests";
        private readonly string DevicePrefix = $"E2E_{nameof(MethodE2EMultiplexingOverAmqpTests)}_";
        private readonly ConsoleEventListener _listener;
        private static TestLogging _log = TestLogging.GetInstance();

        public MethodE2EMultiplexingOverAmqpTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize, 
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponse_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponse_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethod
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.Device,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithoutPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithoutPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithoutPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_Amqp()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_Tcp_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_IoTHubSak_DeviceReceivesMethodAndResponseWithDefaultHandler_MuxWithPooling_AmqpWs()
        {
            await SendMethodAndRespondMuxedOverAmqp(
                ConnectionStringAuthScope.IoTHub,
                Client.TransportType.Amqp_WebSocket_Only,
                MultiplexingOverAmqp.MuxWithPoolingPoolSize,
                MultiplexingOverAmqp.MuxWithPoolingDevicesCount,
                MethodE2ETests.SetDeviceReceiveMethodDefaultHandler
                ).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondMuxedOverAmqp(
            ConnectionStringAuthScope authScope,
            Client.TransportType transport,
            int poolSize,
            int devicesCount,
            Func<DeviceClient, string, Task<Task>> setDeviceReceiveMethod
            )
        {
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MethodE2EMultiplexingOverAmqpTests)}: Setting method for device {testDevice.Id}");
                Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            };

            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                _log.WriteLine($"{nameof(MethodE2EMultiplexingOverAmqpTests)}: Preparing to receive method for device {testDevice.Id}");
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

            await MultiplexingOverAmqp.TestMultiplexingOperationAsync(
                DevicePrefix,
                transport,
                poolSize,
                devicesCount,
                initOperation,
                testOperation,
                cleanupOperation
                ).ConfigureAwait(false);
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
