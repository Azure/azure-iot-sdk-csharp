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
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private const string MethodName = "MethodE2ETest";
        private const string DevicePrefix = "E2E_Method_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MethodE2ETests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only, SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only, SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethod).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodObsoleteHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only, SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithDefaultMethodHandler_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only, SetDeviceReceiveMethodDefaultHandler).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, 
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponse(string deviceName, string methodName, string respJson, string reqJson)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            _log.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponse)}: Invoke method {methodName}.");
            CloudToDeviceMethodResult response = 
                await serviceClient.InvokeDeviceMethodAsync(
                    deviceName,
                    new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(reqJson)).ConfigureAwait(false);

            _log.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponse)}: Method status: {response.Status}.");
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(respJson, response.GetPayloadAsJson());
            
            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task<Task> SetDeviceReceiveMethod(DeviceClient deviceClient)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();


            await deviceClient.SetMethodHandlerAsync(MethodName,
                (request, context) =>
                {
                    _log.WriteLine($"{nameof(SetDeviceReceiveMethod)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");
                    Assert.AreEqual(MethodName, request.Name);
                    Assert.AreEqual(ServiceRequestJson, request.DataAsJson);

                    methodCallReceived.SetResult(true);
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);
            
            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        private async Task<Task> SetDeviceReceiveMethodDefaultHandler(DeviceClient deviceClient)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

            await deviceClient.SetMethodDefaultHandlerAsync(
                (request, context) =>
                {
                    _log.WriteLine($"{nameof(SetDeviceReceiveMethodDefaultHandler)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");

                    Assert.AreEqual(MethodName, request.Name);
                    Assert.AreEqual(ServiceRequestJson, request.DataAsJson);

                    methodCallReceived.SetResult(true);
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            return methodCallReceived.Task;
        }

        private Task<Task> SetDeviceReceiveMethodObsoleteHandler(DeviceClient deviceClient)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();

#pragma warning disable CS0618
            deviceClient.SetMethodHandler(MethodName, (request, context) =>
            {
                _log.WriteLine($"{nameof(SetDeviceReceiveMethodObsoleteHandler)}: DeviceClient method: {request.Name} {request.ResponseTimeout}.");
                
                Assert.AreEqual(MethodName, request.Name);
                Assert.AreEqual(ServiceRequestJson, request.DataAsJson);

                methodCallReceived.SetResult(true);
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
            }, null);
#pragma warning restore CS0618

            return Task.FromResult<Task>(methodCallReceived.Task);
        }

        private async Task SendMethodAndRespond(Client.TransportType transport, Func<DeviceClient, Task<Task>> setDeviceReceiveMethod)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient).ConfigureAwait(false);

            await Task.WhenAll(
                ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson),
                methodReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                Task methodReceivedTask = await SetDeviceReceiveMethod(deviceClient).ConfigureAwait(false);

                await Task.WhenAll(
                    ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson),
                    methodReceivedTask).ConfigureAwait(false);
            };

            await FaultInjection.TestErrorInjectionTemplate(
                DevicePrefix,
                TestDeviceType.Sasl,
                transport,
                faultType,
                reason,
                delayInSec,
                FaultInjection.DefaultDelayInSec,
                (d, t) => { return Task.FromResult<bool>(false); },
                testOperation,
                () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
