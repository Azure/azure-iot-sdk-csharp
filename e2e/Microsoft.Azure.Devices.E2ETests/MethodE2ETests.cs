// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public class MethodE2ETests
    {
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private const string MethodName = "MethodE2ETest";
        private const string DevicePrefix = "E2E_Method_CSharp_";

        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment(DevicePrefix);
            hubConnectionString = environment.Item1;
            registryManager = environment.Item2;
            hostName = TestUtil.GetHostName(hubConnectionString);
        }

        [ClassCleanup]
        static public void ClassCleanup()
        {
            TestUtil.UnInitializeEnvironment(registryManager);
        }
        
        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_Mqtt()
        {
            await sendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponseWithObseletedSetMethodHandler_MqttWs()
        {
            await sendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType.Mqtt_WebSocket_Only);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only, 
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownMqtt,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }


#if WIP_C2D_METHODS_AMQP
        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespond(Client.TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_Tcp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec)
            ;
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpConn,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, 
                TestUtil.FaultType_AmqpConn,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpSess,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpSess,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpMethodReq,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpMethodReq,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_AmqpMethodResp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_AmqpMethodResp,
                TestUtil.FaultCloseReason_Boom,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                TestUtil.FaultType_GracefulShutdownAmqp,
                TestUtil.FaultCloseReason_Bye,
                TestUtil.DefaultDelayInSec);
        }
#endif
        private async Task ServiceSendMethodAndVerifyResponse(string deviceName, string methodName, string respJson, string reqJson, TaskCompletionSource<Tuple<bool, bool>> rel)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            Task<CloudToDeviceMethodResult> directResponseFuture = serviceClient.InvokeDeviceMethodAsync(
                deviceName,
                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(reqJson)
            );
            CloudToDeviceMethodResult response = await directResponseFuture;
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(respJson, response.GetPayloadAsJson());
            Assert.IsTrue(rel.Task.Result.Item1, "Method name is not matching with the send data");
            Assert.IsTrue(rel.Task.Result.Item2, "Json data is not matching with the send data");

            await serviceClient.CloseAsync();
        }

        private async Task SendMethodAndRespond(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.SetMethodHandlerAsync(MethodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null);

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task sendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType transport)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);
            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            deviceClient?.SetMethodHandler(MethodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null);

            // sleep to ensure async tasks started in SetMethodHandler has completed
            Thread.Sleep(5000);

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task SendMethodAndRespondRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice(DevicePrefix, hostName, registryManager);

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);

            ConnectionStatus? lastConnectionStatus = null;
            ConnectionStatusChangeReason? lastConnectionStatusChangeReason = null;
            int setConnectionStatusChangesHandlerCount = 0;

            bool assertStatusChange = false;
            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                if (assertStatusChange) { 
                    if (setConnectionStatusChangesHandlerCount == 0)
                    {
                        Assert.AreEqual(ConnectionStatus.Disconnected_Retrying, status);
                        Assert.AreEqual(ConnectionStatusChangeReason.No_Network, statusChangeReason);
                    }
                    else
                    {
                        Assert.AreEqual(ConnectionStatus.Connected, status);
                        Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, statusChangeReason);
                    }
                }

                lastConnectionStatus = status;
                lastConnectionStatusChangeReason = statusChangeReason;
                setConnectionStatusChangesHandlerCount++;
            });

            await deviceClient.SetMethodHandlerAsync(MethodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null);

            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
            }

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult);

            // allow time for connection recovery
            setConnectionStatusChangesHandlerCount = 0;
            assertStatusChange = true;
            
            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.Minutes < 3 && setConnectionStatusChangesHandlerCount < 2)
            {
                await Task.Delay(1000);
            }

            assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson, ServiceRequestJson, assertResult);
            setConnectionStatusChangesHandlerCount = 0;

            assertStatusChange = false;
            await deviceClient.SetMethodHandlerAsync(MethodName, null, null);

            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Disabled, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Client_Close, lastConnectionStatusChangeReason);
            }

            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}
