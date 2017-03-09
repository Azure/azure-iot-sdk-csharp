using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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
            await sendMethodAndRespond(Client.TransportType.Mqtt_Tcp_Only);
        }

        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await sendMethodAndRespond(Client.TransportType.Mqtt_WebSocket_Only);
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

#if WIP_C2D_METHODS_AMQP
        [TestMethod]
        [TestCategory("Method-E2E")]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp()
        {
            await sendMethodAndRespond(Client.TransportType.Amqp);
        }
#endif

        async Task sendMethodAndRespond(Client.TransportType transport)
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

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            Task<CloudToDeviceMethodResult> directResponseFuture = serviceClient.InvokeDeviceMethodAsync(
                deviceInfo.Item1,
                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(serviceRequestJson)
            );
            Assert.IsTrue(assertResult.Task.Result.Item1, "Method name is not matching with the send data");
            Assert.IsTrue(assertResult.Task.Result.Item2, "Json data is not matching with the send data");
            CloudToDeviceMethodResult response = await directResponseFuture;
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(deviceResponseJson, response.GetPayloadAsJson());

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }

        async Task sendMethodAndRespondWithObseletedSetMethodHandler(Client.TransportType transport)
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

            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            Task<CloudToDeviceMethodResult> directResponseFuture = serviceClient.InvokeDeviceMethodAsync(
                deviceInfo.Item1,
                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(serviceRequestJson)
            );
            Assert.IsTrue(assertResult.Task.Result.Item1, "Method name is not matching with the send data");
            Assert.IsTrue(assertResult.Task.Result.Item2, "Json data is not matching with the send data");
            CloudToDeviceMethodResult response = await directResponseFuture;
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual(deviceResponseJson, response.GetPayloadAsJson());

            await deviceClient.CloseAsync();
            TestUtil.RemoveDevice(deviceInfo.Item1, registryManager);
        }
    }
}
