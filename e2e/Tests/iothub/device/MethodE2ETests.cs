// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Specialized;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Methods
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class MethodE2ETests : E2EMsTestBase
    {
        private static readonly DirectMethodResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        private static readonly DirectMethodRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private readonly string _modulePrefix = $"{nameof(MethodE2ETests)}_mod_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceReceivesMethodAndResponse_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendDeviceMethodAndRespondAsync(new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceUnsubscribes_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(protocol), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceReceivesMethodAndResponse_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendDeviceMethodAndRespondAsync(new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceUnsubscribes_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(protocol), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ModuleReceivesMethodAndResponse_Mqtt(IotHubClientTransportProtocol protocol)
        {
            await SendModuleMethodAndRespondAsync(new IotHubClientMqttSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ModuleReceivesMethodAndResponse_Amqp(IotHubClientTransportProtocol protocol)
        {
            await SendModuleMethodAndRespondAsync(new IotHubClientAmqpSettings(protocol)).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // arrange
            const string methodName = "Reboot";
            bool deviceMethodCalledSuccessfully = false;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync("NullMethodPayloadTest").ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);

            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (methodRequest) =>
                    {
                        methodRequest.MethodName.Should().Be(methodName);
                        deviceMethodCalledSuccessfully = true;
                        var response = new DirectMethodResponse(200);

                        return Task.FromResult(response);
                    })
                .ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ConnectionTimeout = TimeSpan.FromMinutes(1),
                ResponseTimeout = TimeSpan.FromMinutes(1),
            };

            // act
            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest)
                .ConfigureAwait(false);

            // assert
            deviceMethodCalledSuccessfully.Should().BeTrue();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithDateTimePayload_DoesNotThrow()
        {
            // arrange
            var date = new DateTimeOffset(638107582284599400, TimeSpan.FromHours(1));
            var responsePayload = new TestDateTime { Iso8601String = date.ToString("o", CultureInfo.InvariantCulture) };

            const string methodName = "GetDateTime";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync("DateTimeMethodPayloadTest").ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync().ConfigureAwait(false);
            try
            {
                await deviceClient
                    .SetDirectMethodCallbackAsync(
                        (methodRequest) =>
                        {
                            methodRequest.MethodName.Should().Be(methodName);
                            deviceMethodCalledSuccessfully = true;
                            var response = new DirectMethodResponse(200) { Payload = responsePayload };

                            return Task.FromResult(response);
                        })
                    .ConfigureAwait(false);

                using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
                var directMethodRequest = new DirectMethodServiceRequest(methodName)
                {
                    ConnectionTimeout = TimeSpan.FromMinutes(1),
                    ResponseTimeout = TimeSpan.FromMinutes(1),
                };

                // act

                DirectMethodClientResponse response = await serviceClient.DirectMethods
                    .InvokeAsync(testDevice.Id, directMethodRequest)
                    .ConfigureAwait(false);
                bool flag = response.TryGetPayload(out TestDateTime actualPayload);
                Action act = () => DateTimeOffset.ParseExact(actualPayload.Iso8601String, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                // assert

                deviceMethodCalledSuccessfully.Should().BeTrue();
                flag.Should().BeTrue();
                responsePayload.Should().BeEquivalentTo(actualPayload);
                act.Should().NotThrow();
            }
            finally
            {
                // clean up

                await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_OpenCloseOpenDeviceReceivesDirectMethods_MqttTcp()
        {
            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientMqttSettings()).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_OpenCloseOpenDeviceReceivesDirectMethods_AmqpTcp()
        {
            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientAmqpSettings()).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodResponseTimeout : responseTimeout;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ResponseTimeout = methodTimeout,
            };

            // act
            Func<Task> act = async () =>
            {
                await serviceClient.DirectMethods.InvokeAsync(deviceId, directMethodRequest).ConfigureAwait(false);
            };

            // assert
            ExceptionAssertions<IotHubServiceException> error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotOnline);
            error.And.IsTransient.Should().BeTrue();
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            string moduleId,
            DirectMethodServiceRequest directMethodRequest,
            T respJson)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName}.");

            DirectMethodClientResponse response = null;
            if (moduleId == null)
            {
                response = await serviceClient.DirectMethods
                    .InvokeAsync(deviceId, directMethodRequest)
                    .ConfigureAwait(false);
            }
            else {
                response = await serviceClient.DirectMethods
                    .InvokeAsync(deviceId, moduleId, directMethodRequest)
                    .ConfigureAwait(false);
            }

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            response.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(respJson));
        }

        public static async Task<Task> SubscribeAndUnsubscribeMethodAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                (request) =>
                {
                    // This test only verifies that unsubscripion works.
                    // For this reason, the direct method subscription callback does not implement any method-specific dispatcher.
                    VerboseTestLogger.WriteLine($"{nameof(SubscribeAndUnsubscribeMethodAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");
                    var response = new DirectMethodResponse(200)
                    {
                        Payload = s_deviceResponsePayload,
                    };

                    return Task.FromResult(response);
                })
                .ConfigureAwait(false);

            await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        private async Task SendMethodAndUnsubscribeAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient,
                string,
                Task<Task>> subscribeAndUnsubscribeMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            await subscribeAndUnsubscribeMethod(deviceClient, MethodName).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyNotReceivedAsync(
                    testDevice.Id,
                    MethodName,
                    responseTimeout,
                    serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        private async Task SendDeviceMethodAndRespondAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload, DirectMethodResponsePayload>(s_deviceResponsePayload);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                null,
                directMethodRequest,
                s_deviceResponsePayload);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        private async Task OpenCloseOpenThenSendMethodAndRespondAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload, DirectMethodResponsePayload>(s_deviceResponsePayload);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                null,
                directMethodRequest,
                s_deviceResponsePayload);

            var testTimeoutTask = Task.Delay(TimeSpan.FromSeconds(20));

            // The device should still be able to receive direct methods even though it was re-opened.
            var testTask = Task.WhenAll(serviceSendTask, methodReceivedTask);

            Task completedTask = await Task.WhenAny(testTask, testTimeoutTask).ConfigureAwait(false);

            if (completedTask == testTimeoutTask)
            {
                using (new AssertionScope())
                {
                    serviceSendTask.IsCompleted.Should().BeTrue("Time out waiting for the service client to get the direct method response.");
                    methodReceivedTask.IsCompleted.Should().BeTrue("Timed out waiting on the device to receive the expected direct method.");
                }
            }
        }

        private async Task SendModuleMethodAndRespondAsync(IotHubClientTransportSettings transportSettings)
        {
            await using TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            using var testModuleCallbackHandler = new TestModuleCallbackHandler(moduleClient, testModule.DeviceId, testModule.Id);
            await testModuleCallbackHandler.SetModuleReceiveMethodAndRespondAsync<DirectMethodRequestPayload, DirectMethodResponsePayload>(s_deviceResponsePayload);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testModuleCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            using var cts = new CancellationTokenSource(s_defaultMethodResponseTimeout);
            Task methodReceivedTask = testModuleCallbackHandler.WaitForMethodCallbackAsync(cts.Token);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testModule.DeviceId,
                testModule.Id,
                directMethodRequest,
                s_deviceResponsePayload);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        internal class TestDateTime
        {
            public string Iso8601String { get; set; }
        }
    }
}
