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
        private const string DevicePrefix = "E2E_Method_CSharp_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MethodE2ETests()
        {
            _listener = new ConsoleEventListener("Microsoft-Azure-");
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Mqtt()
        {
            await SendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_MqttWs()
        {
            await SendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithSetMethodHandler_Mqtt()
        {
            await SendMethodAndRespondWithSetMethodHandler(Client.TransportType.Mqtt_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseWithSetMethodHandler_MqttWs()
        {
            await SendMethodAndRespondWithSetMethodHandler(Client.TransportType.Mqtt_WebSocket_Only).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] // TODO: #558
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
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
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

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, 
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [Ignore] //TODO: #571
        [TestMethod]
        [TestCategory("IoTHub-FaultInjection")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponse(string deviceName, string methodName, string respJson, string reqJson, TaskCompletionSource<Tuple<bool, bool>> rel)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            Task<CloudToDeviceMethodResult> directResponseFuture = serviceClient.InvokeDeviceMethodAsync(
                deviceName,
                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(reqJson)
            );
            CloudToDeviceMethodResult response = await directResponseFuture.ConfigureAwait(false);
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(respJson, response.GetPayloadAsJson());
            Assert.IsTrue(rel.Task.Result.Item1, "Method name is not matching with the send data");
            Assert.IsTrue(rel.Task.Result.Item2, "Json data is not matching with the send data");

            await serviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespond(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            await deviceClient.SetMethodHandlerAsync(MethodName,
                (request, context) =>
                {
                    assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondWithSetMethodHandler(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            
            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);
            
            await (deviceClient?.SetMethodHandlerAsync(MethodName, (request, context) => 
            {
                assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
            }, null)).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType transport)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

#pragma warning disable CS0618

            deviceClient?.SetMethodHandler(MethodName, (request, context) =>
            {
                assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
            }, null);
#pragma warning restore CS0618

            // sleep to ensure async tasks started in SetMethodHandler has completed
            Thread.Sleep(5000);

            await ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(testDevice.ConnectionString, transport);

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int setConnectionStatusChangesHandlerCount = 0;
            var tcsConnected = new TaskCompletionSource<bool>();
            var tcsDisconnected = new TaskCompletionSource<bool>();

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                _log.WriteLine("Connection Changed to {0} because {1}", status, statusChangeReason);
                
                if (status == ConnectionStatus.Disconnected_Retrying)
                {
                    tcsDisconnected.TrySetResult(true);
                    Assert.AreEqual(ConnectionStatusChangeReason.No_Network, statusChangeReason);
                }
                else if (status == ConnectionStatus.Connected)
                {
                    tcsConnected.TrySetResult(true);
                }

                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                setConnectionStatusChangesHandlerCount++;
            });

            await deviceClient.SetMethodHandlerAsync(MethodName,
                (request, context) =>
                {
                    assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName),
                        request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null).ConfigureAwait(false);

            // assert on successful connection
            await Task.WhenAny(Task.Delay(FaultInjection.ShortRetryInMilliSec), tcsConnected.Task).ConfigureAwait(false);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Initial connection failed");

            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
            }

            // check on normal operation
            await
                ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson,
                    ServiceRequestJson, assertResult).ConfigureAwait(false);

            // reset ConnectionStatusChangesHandler data
            setConnectionStatusChangesHandlerCount = 0;
            tcsConnected = new TaskCompletionSource<bool>();
            tcsDisconnected = new TaskCompletionSource<bool>();

            // send error command
            await deviceClient.SendEventAsync(FaultInjection.ComposeErrorInjectionProperties(faultType, reason, delayInSec)).ConfigureAwait(false);

            // wait for disconnection
            await Task.WhenAny(Task.Delay(FaultInjection.WaitForDisconnectMilliseconds), tcsDisconnected.Task).ConfigureAwait(false);
            Assert.IsTrue(tcsDisconnected.Task.IsCompleted, "Error injection did not interrupt the device");

            await Task.WhenAny(Task.Delay(FaultInjection.RecoveryTimeMilliseconds), tcsConnected.Task).ConfigureAwait(false);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Recovery connection failed");

            assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            await
                ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson,
                    ServiceRequestJson, assertResult).ConfigureAwait(false);
            setConnectionStatusChangesHandlerCount = 0;

            //remove and CloseAsync
            await deviceClient.SetMethodHandlerAsync(MethodName, null, null).ConfigureAwait(false);

            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Disabled, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, lastConnectionStatusChangeReason);
            }
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
