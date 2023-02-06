// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Text;
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
    [TestCategory("IoTHub")]
    public class MethodE2ETests : E2EMsTestBase
    {
        internal static readonly DeviceResponsePayload s_deviceResponsePayload = new() { CurrentState = "on" };
        internal static readonly ServiceRequestPayload s_serviceRequestPayload = new() { DesiredState = "off" };

        private readonly string _devicePrefix = $"{nameof(MethodE2ETests)}_dev_";
        private readonly string _modulePrefix = $"{nameof(MethodE2ETests)}_mod_";
        private const string MethodName = "MethodE2ETest";

        private static readonly TimeSpan s_defaultMethodTimeoutMinutes = TimeSpan.FromMinutes(1);

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_MqttTcp()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_MqttWs()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_AmqpTcp()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_AmqpWs()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceSendsMethodThroughProxyWithDefaultTimeout()
        {
            var serviceClientTransportSettings = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    new IotHubClientMqttSettings(),
                    SetDeviceReceiveMethodAsync,
                    serviceClientTransportSettings: serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceSendsMethodThroughProxyWithCustomTimeout()
        {
            var serviceClientTransportSettings = new IotHubServiceClientOptions
            {
                Proxy = new WebProxy(TestConfiguration.IotHub.ProxyServerAddress)
            };

            await SendMethodAndRespondAsync(
                    new IotHubClientMqttSettings(),
                    SetDeviceReceiveMethodAsync,
                    TimeSpan.FromMinutes(5),
                    serviceClientTransportSettings)
                .ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownDeviceThrows()
        {
            // setup
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var methodInvocation = new DirectMethodServiceRequest("SetTelemetryInterval")
            {
                Payload = "10"
            };

            // act
            // Invoke the direct method asynchronously and get the response from the simulated device.
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync("SomeNonExistantDevice", methodInvocation);

            // assert
            ExceptionAssertions<IotHubServiceException> error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownModuleThrows()
        {
            // setup
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync("ModuleNotFoundTest").ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var directMethodRequest = new DirectMethodServiceRequest("SetTelemetryInterval")
            {
                Payload = "10",
            };

            // act
            // Invoke the direct method asynchronously and get the response from the simulated device.
            Func<Task> act = async () =>
                await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, "someNonExistantModuleOnAnExistingDevice", directMethodRequest).ConfigureAwait(false);

            // assert
            ExceptionAssertions<IotHubServiceException> error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.ModuleNotFound);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // arrange

            const string methodName = "Reboot";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync("NullMethodPayloadTest").ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient
                    .SetDirectMethodCallbackAsync(
                        (methodRequest) =>
                        {
                            methodRequest.MethodName.Should().Be(methodName);
                            deviceMethodCalledSuccessfully = true;
                            var response = new Client.DirectMethodResponse(200);

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
            finally
            {
                // clean up

                await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
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
            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
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
            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [TestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_OpenCloseOpenDeviceReceivesDirectMethods_AmqpTcp()
        {
            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
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
            string methodName,
            T respJson,
            object reqJson,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ResponseTimeout = methodTimeout,
                Payload = reqJson,
            };

            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            response.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(respJson));
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync<T>(
            string deviceId,
            string moduleId,
            string methodName,
            T respJson,
            object reqJson,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;

            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ResponseTimeout = methodTimeout,
                Payload = reqJson,
            };

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, moduleId, directMethodRequest)
                .ConfigureAwait(false);

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
                    var response = new Client.DirectMethodResponse(200)
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

        public static async Task<Task> SetDeviceReceiveMethodAsync(IotHubDeviceClient deviceClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                        {
                            VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

                            try
                            {
                                try
                                {
                                    request.MethodName.Should().Be(methodName);
                                    request.TryGetPayload(out ServiceRequestPayload requestPayload).Should().BeTrue();
                                    requestPayload.Should().BeEquivalentTo(s_serviceRequestPayload);
                                }
                                catch (Exception ex)
                                {
                                    methodCallReceived.TrySetException(ex);
                                }
                                var response = new Client.DirectMethodResponse(200)
                                {
                                    Payload = s_deviceResponsePayload,
                                };

                                return Task.FromResult(response);
                            }
                            finally
                            {
                                methodCallReceived.TrySetResult(true);
                            }
                        })
                .ConfigureAwait(false);

            // Return the task that tells us we have received the callback.
            return methodCallReceived.Task;
        }

        public static async Task<Task> SetModuleReceiveMethodAsync(IotHubModuleClient moduleClient, string methodName)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await moduleClient.OpenAsync().ConfigureAwait(false);
            await moduleClient.SetDirectMethodCallbackAsync(
                (request) =>
                {
                    VerboseTestLogger.WriteLine($"{nameof(SetDeviceReceiveMethodAsync)}: ModuleClient method: {request.MethodName} {request.ResponseTimeout}.");

                    try
                    {
                        try
                        {
                            request.MethodName.Should().Be(methodName);
                            request.TryGetPayload(out ServiceRequestPayload requestPayload).Should().BeTrue();
                            requestPayload.Should().BeEquivalentTo(s_serviceRequestPayload);
                        }
                        catch (Exception ex)
                        {
                            methodCallReceived.TrySetException(ex);
                        }

                        var response = new Client.DirectMethodResponse(200)
                        {
                            Payload = s_deviceResponsePayload,
                        };

                        return Task.FromResult(response);
                    }
                    finally
                    {
                        methodCallReceived.TrySetResult(true);
                    }
                }).ConfigureAwait(false);

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
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
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

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                s_deviceResponsePayload,
                s_serviceRequestPayload,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        private async Task OpenCloseOpenThenSendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient.CloseAsync().ConfigureAwait(false);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName).ConfigureAwait(false);

            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                s_deviceResponsePayload,
                s_serviceRequestPayload,
                responseTimeout,
                serviceClientTransportSettings);

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

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubModuleClient, string, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(moduleClient, MethodName).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendMethodAndVerifyResponseAsync(
                        testModule.DeviceId,
                        testModule.Id,
                        MethodName,
                        s_deviceResponsePayload,
                        s_serviceRequestPayload,
                        responseTimeout,
                        serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);
        }

        internal class DeviceResponsePayload
        {
            [JsonProperty("currentState")]
            public string CurrentState { get; set; }
        }

        internal class ServiceRequestPayload
        {
            [JsonProperty("desiredState")]
            public string DesiredState { get; set; }
        }

        internal class TestDateTime
        {
            public string Iso8601String { get; set; }
        }
    }
}
