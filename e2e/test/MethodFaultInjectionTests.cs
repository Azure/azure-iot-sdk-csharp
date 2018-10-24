// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    [TestCategory("IoTHub-FaultInjection")]
    public class MethodFaultInjectionTests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(MethodFaultInjectionTests)}_";
        private const string DeviceResponseJson = "{\"name\":\"e2e_test\"}";
        private const string ServiceRequestJson = "{\"a\":123}";
        private const string MethodName = "MethodE2ETest";
        private static TestLogging s_log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public MethodFaultInjectionTests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceReceivesMethodAndResponseRecovery_Mqtt()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_MqttWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Mqtt_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownMqtt,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodTcpConnRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodTcpConnRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_Tcp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodAmqpConnLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpConn,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodSessionLostRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodSessionLostRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpSess,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodReqLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodReqLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodReq,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodRespLinkDropRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodRespLinkDropRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_AmqpMethodResp,
                FaultInjection.FaultCloseReason_Boom,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_Amqp()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_Tcp_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_DeviceMethodGracefulShutdownRecovery_AmqpWs()
        {
            await SendMethodAndRespondRecovery(Client.TransportType.Amqp_WebSocket_Only,
                FaultInjection.FaultType_GracefulShutdownAmqp,
                FaultInjection.FaultCloseReason_Bye,
                FaultInjection.DefaultDelayInSec).ConfigureAwait(false);
        }

        private async Task ServiceSendMethodAndVerifyResponse(string deviceName, string methodName, string respJson, string reqJson)
        {
            var sw = new Stopwatch();
            sw.Start();
            bool done = false;
            ExceptionDispatchInfo exceptionDispatchInfo = null;

            while (!done && sw.ElapsedMilliseconds < 3000) //FaultInjection.RecoveryTimeMilliseconds)
            {
                try
                {
                    using (ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
                    {
                        s_log.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponse)}: Invoke method {methodName}.");
                        CloudToDeviceMethodResult response =
                            await serviceClient.InvokeDeviceMethodAsync(
                                deviceName,
                                new CloudToDeviceMethod(methodName, TimeSpan.FromMinutes(5)).SetPayloadJson(reqJson)).ConfigureAwait(false);

                        s_log.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponse)}: Method status: {response.Status}.");
                        Assert.AreEqual(200, response.Status);
                        Assert.AreEqual(respJson, response.GetPayloadAsJson());
                        
                        await serviceClient.CloseAsync().ConfigureAwait(false);
                        done = true;
                    }
                }
                catch (DeviceNotFoundException ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                    s_log.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponse)}: ServiceClient exception caught: {ex}.");
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }

            if (exceptionDispatchInfo != null)
            {
                exceptionDispatchInfo.Throw();
            }
        }

        private async Task SendMethodAndRespondRecovery(Client.TransportType transport, string faultType, string reason, int delayInSec)
        {
            TestDeviceCallbackHandler testDeviceCallbackHandler = null;
            var cts = new CancellationTokenSource(FaultInjection.RecoveryTimeMilliseconds);

            // Configure the callback and start accepting method calls.
            Func<DeviceClient, TestDevice, Task> initOperation = async (deviceClient, testDevice) =>
            {
                testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient);
                await testDeviceCallbackHandler.SetDeviceReceiveMethodAsync(MethodName, DeviceResponseJson, ServiceRequestJson).ConfigureAwait(false);
            };

            // Call the method from the service side and verify the device received the call.
            Func<DeviceClient, TestDevice, Task> testOperation = async (deviceClient, testDevice) =>
            {
                Task serviceSendTask = ServiceSendMethodAndVerifyResponse(testDevice.Id, MethodName, DeviceResponseJson, ServiceRequestJson);
                Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);

                var tasks = new List<Task>() { serviceSendTask, methodReceivedTask };
                while (tasks.Count > 0)
                {
                    Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                    completedTask.GetAwaiter().GetResult();
                    tasks.Remove(completedTask);
                }
            };

            // This is a simple hack to ensure that we're still exercising these tests while we address the race conditions causing the following issues:
            // [Ignore] // TODO: #571
            // [Ignore] // TODO: #558
            int retryCount = 3;

            while (retryCount-- > 0)
            {
                try
                {
                    await FaultInjection.TestErrorInjectionAsync(
                        DevicePrefix,
                        TestDeviceType.Sasl,
                        transport,
                        faultType,
                        reason,
                        delayInSec,
                        FaultInjection.DefaultDelayInSec,
                        initOperation,
                        testOperation,
                        () => { return Task.FromResult<bool>(false); }).ConfigureAwait(false);
                }
                catch (DeviceNotFoundException e)
                {
                    s_log.WriteLine($"Retrying fault injection test ({retryCount} left): {e}");
                }
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
