// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class MethodE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MethodE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MethodE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, MethodOperation.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, MethodOperation.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, MethodOperation.SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, MethodOperation.SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, MethodOperation.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, MethodOperation.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, MethodOperation.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, MethodOperation.SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, MethodOperation.SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, MethodOperation.SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, MethodOperation.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, MethodOperation.SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        private async Task SendMethodAndRespond(Client.TransportType transport, Func<DeviceClient, Task<Task>> setDeviceReceiveMethod)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            using (DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport))
            {
                Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient).ConfigureAwait(false);

                await Task.WhenAll(
                    MethodOperation.ServiceSendMethodAndVerifyResponse(testDevice.Id),
                    methodReceivedTask).ConfigureAwait(false);

                await deviceClient.CloseAsync().ConfigureAwait(false);
            }
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
