// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
        Justification = "Uses custom scheme for cleanup")]
    public class MethodE2ETests
    {
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private const string MethodName = "MethodE2ETest";
        private const string DevicePrefix = "E2E_Method_CSharp_";

        private static string hubConnectionString;
        private static string hostName;
        private static RegistryManager registryManager;

        private readonly SemaphoreSlim sequentialTestSemaphore = new SemaphoreSlim(1, 1);

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

#if NETSTANDARD1_3
        [TestInitialize]
        public async Task Initialize()
        {
            await sequentialTestSemaphore.WaitAsync();
        }
#else
        [TestInitialize]
        public void Initialize()
        {
            sequentialTestSemaphore.Wait();
        }
#endif

        [TestCleanup]
        public void Cleanup()
        {
            sequentialTestSemaphore.Release(1);
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

        [Ignore]
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

        [Ignore]
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

        [Ignore]
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

        [Ignore]
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

        [Ignore]
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

        [Ignore]
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

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
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

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
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

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
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

        [Ignore] //TODO: #194 Test intermittently failing on Windows.
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

        [Ignore]
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

        [Ignore]
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
                    assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
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

// TODO: #193
// DeviceClient.SetMethodHandler(string, MethodCallback, object)' is obsolete: 'Please use SetMethodHandlerAsync.
#pragma warning disable CS0618
            deviceClient?.SetMethodHandler(MethodName,
                (request, context) =>
                {
                    assertResult.TrySetResult(new Tuple<bool, bool>(request.Name.Equals(MethodName), request.DataAsJson.Equals(ServiceRequestJson)));
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(DeviceResponseJson), 200));
                },
                null);
#pragma warning restore CS0618

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
            var tcsConnected = new TaskCompletionSource<bool>();
            var tcsDisconnected = new TaskCompletionSource<bool>();

            deviceClient.SetConnectionStatusChangesHandler((status, statusChangeReason) =>
            {
                Debug.WriteLine("Connection Changed to {0} because {1}", status, statusChangeReason);
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
                null);

            // assert on successfuly connection
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                }), tcsConnected.Task);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Initial connection failed");
            if (transport != Client.TransportType.Http1)
            {
                Assert.AreEqual(1, setConnectionStatusChangesHandlerCount);
                Assert.AreEqual(ConnectionStatus.Connected, lastConnectionStatus);
                Assert.AreEqual(ConnectionStatusChangeReason.Connection_Ok, lastConnectionStatusChangeReason);
            }

            // check on normal operation
            await
                ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson,
                    ServiceRequestJson, assertResult);

            // reset ConnectionStatusChangesHandler data
            setConnectionStatusChangesHandlerCount = 0;
            tcsConnected = new TaskCompletionSource<bool>();
            tcsDisconnected = new TaskCompletionSource<bool>();

            // send error command
            await deviceClient.SendEventAsync(TestUtil.ComposeErrorInjectionProperties(faultType, reason, delayInSec));

            // wait for disconnection
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }), tcsDisconnected.Task);
            Assert.IsTrue(tcsDisconnected.Task.IsCompleted, "Error injection did not interrupt the device");

            // allow max 3 minutes for connection recovery
            await Task.WhenAny(
                Task.Run(async () =>
                {
                    ////////////////////////change it to 30
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    return Task.FromResult(true);
                }), tcsConnected.Task);
            Assert.IsTrue(tcsConnected.Task.IsCompleted, "Recovery connection failed");

            assertResult = new TaskCompletionSource<Tuple<bool, bool>>();
            await
                ServiceSendMethodAndVerifyResponse(deviceInfo.Item1, MethodName, DeviceResponseJson,
                    ServiceRequestJson, assertResult);
            setConnectionStatusChangesHandlerCount = 0;

            //remove and CloseAsync
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
