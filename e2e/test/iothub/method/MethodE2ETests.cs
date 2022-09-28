// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
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

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_MqttTcp()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_MqttWs()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SetDeviceReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_AmqpTcp()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_DeviceUnsubscribes_AmqpWs()
        {
            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SubscribeAndUnsubscribeMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
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

        [LoggedTestMethod]
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

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownDeviceThrows()
        {
            // setup
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var methodInvocation = new DirectMethodRequest
            {
                MethodName = "SetTelemetryInterval",
                Payload = "10"
            };

            // act
            // Invoke the direct method asynchronously and get the response from the simulated device.
            Func<Task> act = async () => await serviceClient.DirectMethods.InvokeAsync("SomeNonExistantDevice", methodInvocation);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotFound);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_MqttWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpTcp()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ModuleReceivesMethodAndResponse_AmqpWs()
        {
            await SendMethodAndRespondAsync(new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket), SetModuleReceiveMethodAsync).ConfigureAwait(false);
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithUnknownModuleThrows()
        {
            // setup
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, "ModuleNotFoundTest").ConfigureAwait(false);
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = "SetTelemetryInterval",
                Payload = "10",
            };

            // act
            // Invoke the direct method asynchronously and get the response from the simulated device.
            Func<Task> act = async () =>
                await serviceClient.DirectMethods.InvokeAsync(testDevice.Id, "someNonExistantModuleOnAnExistingDevice", directMethodRequest).ConfigureAwait(false);

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.ModuleNotFound);
            error.And.IsTransient.Should().BeFalse();
        }

        [LoggedTestMethod]
        [Timeout(TestTimeoutMilliseconds)]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // arrange

            const string commandName = "Reboot";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, "NullMethodPayloadTest").ConfigureAwait(false);
            using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            try
            {
                await deviceClient.OpenAsync().ConfigureAwait(false);
                await deviceClient
                    .SetDirectMethodCallbackAsync(
                        (methodRequest) =>
                        {
                            methodRequest.MethodName.Should().Be(commandName);
                            deviceMethodCalledSuccessfully = true;
                            var response = new Client.DirectMethodResponse(200);

                            return Task.FromResult(response);
                        })
                    .ConfigureAwait(false);

                using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
                var directMethodRequest = new DirectMethodRequest
                {
                    MethodName = commandName,
                    ConnectionTimeout = TimeSpan.FromMinutes(1),
                    ResponseTimeout = TimeSpan.FromMinutes(1),
                };

                // act

                DirectMethodResponse response = await serviceClient.DirectMethods
                    .InvokeAsync(testDevice.Id, directMethodRequest)
                    .ConfigureAwait(false);

                // assert

                deviceMethodCalledSuccessfully.Should().BeTrue();
            }
            finally
            {
                // clean up

                await deviceClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
                await deviceClient.CloseAsync().ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync().ConfigureAwait(false);
            }
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                ResponseTimeout = methodTimeout,
            };

            // act
            Func<Task> act = async () =>
            {
                await serviceClient.DirectMethods.InvokeAsync(deviceId, directMethodRequest).ConfigureAwait(false);
            };

            // assert
            var error = await act.Should().ThrowAsync<IotHubServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.And.ErrorCode.Should().Be(IotHubServiceErrorCode.DeviceNotOnline);
            error.And.IsTransient.Should().BeTrue();
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string methodName,
            object respJson,
            object reqJson,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;
            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                ResponseTimeout = methodTimeout,
                Payload = reqJson,
            };

            DirectMethodResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, directMethodRequest)
                .ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            JsonConvert.SerializeObject(response.Payload).Should().Be(JsonConvert.SerializeObject(respJson));
        }

        public static async Task ServiceSendMethodAndVerifyResponseAsync(
            string deviceId,
            string moduleId,
            string methodName,
            object respJson,
            object reqJson,
            MsTestLogger logger,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            TimeSpan methodTimeout = responseTimeout == default ? s_defaultMethodTimeoutMinutes : responseTimeout;

            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
                ResponseTimeout = methodTimeout,
                Payload = reqJson,
            };

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");
            DirectMethodResponse response = await serviceClient.DirectMethods
                .InvokeAsync(deviceId, moduleId, directMethodRequest)
                .ConfigureAwait(false);

            logger.Trace($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            JsonConvert.SerializeObject(response.Payload).Should().Be(JsonConvert.SerializeObject(respJson));
        }

        public static async Task<Task> SubscribeAndUnsubscribeMethodAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                (request) =>
                {
                    // This test only verifies that unsubscripion works.
                    // For this reason, the direct method subscription callback does not implement any method-specific dispatcher.
                    logger.Trace($"{nameof(SubscribeAndUnsubscribeMethodAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");
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

        public static async Task<Task> SetDeviceReceiveMethodAsync(IotHubDeviceClient deviceClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>();
            await deviceClient.OpenAsync().ConfigureAwait(false);
            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (request) =>
                        {
                            logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");

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

        public static async Task<Task> SetModuleReceiveMethodAsync(IotHubModuleClient moduleClient, string methodName, MsTestLogger logger)
        {
            var methodCallReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await moduleClient.OpenAsync().ConfigureAwait(false);
            await moduleClient.SetDirectMethodCallbackAsync(
                (request) =>
                {
                    logger.Trace($"{nameof(SetDeviceReceiveMethodAsync)}: ModuleClient method: {request.MethodName} {request.ResponseTimeout}.");

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
                string, MsTestLogger,
                Task<Task>> subscribeAndUnsubscribeMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            await subscribeAndUnsubscribeMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyNotReceivedAsync(
                    testDevice.Id,
                    MethodName,
                    Logger,
                    responseTimeout,
                    serviceClientTransportSettings)
                .ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubDeviceClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(Logger, _devicePrefix).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(deviceClient, MethodName, Logger).ConfigureAwait(false);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                MethodName,
                s_deviceResponsePayload,
                s_serviceRequestPayload,
                Logger,
                responseTimeout,
                serviceClientTransportSettings);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);

            await deviceClient.CloseAsync().ConfigureAwait(false);
        }

        private async Task SendMethodAndRespondAsync(
            IotHubClientTransportSettings transportSettings,
            Func<IotHubModuleClient, string, MsTestLogger, Task<Task>> setDeviceReceiveMethod,
            TimeSpan responseTimeout = default,
            IotHubServiceClientOptions serviceClientTransportSettings = default)
        {
            TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, Logger).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            await moduleClient.OpenAsync().ConfigureAwait(false);

            Task methodReceivedTask = await setDeviceReceiveMethod(moduleClient, MethodName, Logger).ConfigureAwait(false);

            await Task
                .WhenAll(
                    ServiceSendMethodAndVerifyResponseAsync(
                        testModule.DeviceId,
                        testModule.Id,
                        MethodName,
                        s_deviceResponsePayload,
                        s_serviceRequestPayload,
                        Logger,
                        responseTimeout,
                        serviceClientTransportSettings),
                    methodReceivedTask)
                .ConfigureAwait(false);

            await moduleClient.CloseAsync().ConfigureAwait(false);
        }

        internal class DeviceResponsePayload
        {
            [JsonProperty(PropertyName = "currentState")]
            public string CurrentState { get; set; }
        }

        internal class ServiceRequestPayload
        {
            [JsonProperty(PropertyName = "desiredState")]
            public string DesiredState { get; set; }
        }
    }
}
