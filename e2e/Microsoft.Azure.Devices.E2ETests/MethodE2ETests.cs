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
        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        static public void ClassInitialize(TestContext testContext)
        {
            var environment = TestUtil.InitializeEnvironment("E2E_Method_CSharp_");
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
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only, "KillTcp", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only, "KillTcp", "boom", 1);
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
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillTcp", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpConnection", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpConnection", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpSession", "boom", 1);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpSession", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpMethodReqLink", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpMethodReqLink", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only, "KillAmqpMethodRespLink", "boom", 1);
        }

        [Ignore]
        [TestMethod]
        [TestCategory("Method-E2E")]
        [TestCategory("Recovery")]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only, "KillAmqpMethodRespLink", "boom", 1);
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
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Method_CSharp_", hostName, registryManager);
            string deviceResponseJson = "{\"name\":\"e2e_test\"}";
            string serviceRequestJson = "{\"a\":123}";
            string methodName = "MethodE2ETest";

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.SetMethodHandlerAsync(methodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(methodName), request.DataAsJson.Equals(serviceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                },
                null);

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, methodName, deviceResponseJson, serviceRequestJson, assertResult);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task sendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType transport)
        {
            string deviceResponseJson = "{\"name\":\"e2e_test\"}";
            string serviceRequestJson = "{\"a\":123}";
            string methodName = "MethodE2ETest";

            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Method_CSharp_", hostName, registryManager);
            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            deviceClient?.SetMethodHandler(methodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(methodName), request.DataAsJson.Equals(serviceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                },
                null);

            // sleep to ensure async tasks started in SetMethodHandler has completed
            Thread.Sleep(5000);

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, methodName, deviceResponseJson, serviceRequestJson, assertResult);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        private async Task SendMethodAndRespondRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            Tuple<string, string> deviceInfo = TestUtil.CreateDevice("E2E_Method_CSharp_", hostName, registryManager);
            string deviceResponseJson = "{\"name\":\"e2e_test\"}";
            string serviceRequestJson = "{\"a\":123}";
            string methodName = "MethodE2ETest";

            var assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceInfo.Item2, transport);
            await deviceClient.SetMethodHandlerAsync(methodName,
                (request, context) =>
                {
                    assertResult.SetResult(new Tuple<bool, bool>(request.Name.Equals(methodName), request.DataAsJson.Equals(serviceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(deviceResponseJson), 200));
                },
                null);

            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, methodName, deviceResponseJson, serviceRequestJson, assertResult);

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));
            Debug.WriteLine("Drop command sent from device client.");

            // allow time for connection recovery
            await Task.Delay(TimeSpan.FromSeconds(3));

            assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            await ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, methodName, deviceResponseJson, serviceRequestJson, assertResult);

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}
