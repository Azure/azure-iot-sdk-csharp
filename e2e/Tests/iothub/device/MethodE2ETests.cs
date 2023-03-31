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

        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceReceivesMethodAndResponds_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendDeviceMethodAndRespondAsync(new IotHubClientMqttSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceUnsubscribes_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndUnsubscribeAsync(new IotHubClientMqttSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceReceivesMethodAndResponds_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendDeviceMethodAndRespondAsync(new IotHubClientAmqpSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_DeviceUnsubscribes_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendMethodAndUnsubscribeAsync(new IotHubClientAmqpSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ModuleReceivesMethodAndResponse_Mqtt(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendModuleMethodAndRespondAsync(new IotHubClientMqttSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public async Task Method_ModuleReceivesMethodAndResponse_Amqp(IotHubClientTransportProtocol protocol)
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await SendModuleMethodAndRespondAsync(new IotHubClientAmqpSettings(protocol), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithNullPayload_DoesNotThrow()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            // arrange
            const string methodName = "Reboot";
            bool deviceMethodCalledSuccessfully = false;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync("NullMethodPayloadTest", ct: ct).ConfigureAwait(false);
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            await deviceClient
                .SetDirectMethodCallbackAsync(
                    (methodRequest) =>
                    {
                        methodRequest.MethodName.Should().Be(methodName);
                        deviceMethodCalledSuccessfully = true;
                        var response = new DirectMethodResponse(200);

                        return Task.FromResult(response);
                    },
                    ct)
                .ConfigureAwait(false);

            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ConnectionTimeout = s_defaultMethodResponseTimeout,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };

            // act
            DirectMethodClientResponse response = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest, ct)
                .ConfigureAwait(false);

            // assert
            deviceMethodCalledSuccessfully.Should().BeTrue();
        }

        [TestMethod]
        public async Task Method_ServiceInvokeDeviceMethodWithDateTimePayload_DoesNotThrow()
        {
            // arrange

            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            var date = new DateTimeOffset(638107582284599400, TimeSpan.FromHours(1));
            var responsePayload = new TestDateTime { Iso8601String = date.ToString("o", CultureInfo.InvariantCulture) };

            const string methodName = "GetDateTime";
            bool deviceMethodCalledSuccessfully = false;
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync("DateTimeMethodPayloadTest", ct: ct).ConfigureAwait(false);
            await using IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(new IotHubClientMqttSettings()));
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);
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
                        },
                        ct)
                    .ConfigureAwait(false);

                using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
                var directMethodRequest = new DirectMethodServiceRequest(methodName)
                {
                    ConnectionTimeout = s_defaultMethodResponseTimeout,
                    ResponseTimeout = s_defaultMethodResponseTimeout,
                };

                // act

                DirectMethodClientResponse response = await serviceClient.DirectMethods
                    .InvokeAsync(testDevice.Id, directMethodRequest, ct)
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

                await deviceClient.SetDirectMethodCallbackAsync(null, ct).ConfigureAwait(false);
                await testDevice.RemoveDeviceAsync(ct).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task Method_OpenCloseOpenDeviceReceivesDirectMethods_MqttTcp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientMqttSettings(), ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Method_OpenCloseOpenDeviceReceivesDirectMethods_AmqpTcp()
        {
            // Setting up one cancellation token for the complete test flow
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;

            await OpenCloseOpenThenSendMethodAndRespondAsync(new IotHubClientAmqpSettings(), ct).ConfigureAwait(false);
        }

        public static async Task ServiceSendMethodAndVerifyNotReceivedAsync(
            string deviceId,
            string methodName,
            CancellationToken ct)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {methodName}.");

            var directMethodRequest = new DirectMethodServiceRequest(methodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };

            // act
            Func<Task> act = async () =>
            {
                await serviceClient.DirectMethods.InvokeAsync(deviceId, directMethodRequest, ct).ConfigureAwait(false);
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
            T respJson,
            CancellationToken ct)
        {
            using var serviceClient = new IotHubServiceClient(TestConfiguration.IotHub.ConnectionString);
            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Invoke method {directMethodRequest.MethodName}.");

            DirectMethodClientResponse response = null;
            if (moduleId == null)
            {
                response = await serviceClient.DirectMethods
                    .InvokeAsync(deviceId, directMethodRequest, ct)
                    .ConfigureAwait(false);
            }
            else {
                response = await serviceClient.DirectMethods
                    .InvokeAsync(deviceId, moduleId, directMethodRequest, ct)
                    .ConfigureAwait(false);
            }

            VerboseTestLogger.WriteLine($"{nameof(ServiceSendMethodAndVerifyResponseAsync)}: Method status: {response.Status}.");
            response.Status.Should().Be(200);
            response.TryGetPayload(out T actual).Should().BeTrue();
            JsonConvert.SerializeObject(actual).Should().Be(JsonConvert.SerializeObject(respJson));
        }

        private async Task SendMethodAndUnsubscribeAsync(
            IotHubClientTransportSettings transportSettings,
            CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            await deviceClient
                .SetDirectMethodCallbackAsync(
                (request) =>
                {
                    // This test only verifies that unsubscripion works.
                    // For this reason, the direct method subscription callback does not implement any method-specific dispatcher.
                    VerboseTestLogger.WriteLine($"{nameof(SendMethodAndUnsubscribeAsync)}: DeviceClient method: {request.MethodName} {request.ResponseTimeout}.");
                    var response = new DirectMethodResponse(200)
                    {
                        Payload = s_deviceResponsePayload,
                    };

                    return Task.FromResult(response);
                },
                ct).ConfigureAwait(false);

            await deviceClient.SetDirectMethodCallbackAsync(null, ct).ConfigureAwait(false);

            await ServiceSendMethodAndVerifyNotReceivedAsync(
                    testDevice.Id,
                    MethodName,
                    ct)
                .ConfigureAwait(false);
        }

        private async Task SendDeviceMethodAndRespondAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                null,
                directMethodRequest,
                s_deviceResponsePayload,
                ct);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        private async Task OpenCloseOpenThenSendMethodAndRespondAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);

            // Close and re-open the client under test.
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);
            await deviceClient.CloseAsync(ct).ConfigureAwait(false);
            await deviceClient.OpenAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            Task methodReceivedTask = testDeviceCallbackHandler.WaitForMethodCallbackAsync(ct);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testDevice.Id,
                null,
                directMethodRequest,
                s_deviceResponsePayload,
                ct);

            var testTimeoutTask = Task.Delay(TimeSpan.FromSeconds(20), ct);

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

        private async Task SendModuleMethodAndRespondAsync(IotHubClientTransportSettings transportSettings, CancellationToken ct)
        {
            await using TestModule testModule = await TestModule.GetTestModuleAsync(_devicePrefix, _modulePrefix, ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(transportSettings);
            await using var moduleClient = new IotHubModuleClient(testModule.ConnectionString, options);
            await moduleClient.OpenAsync(ct).ConfigureAwait(false);

            using var testModuleCallbackHandler = new TestModuleCallbackHandler(moduleClient, testModule.DeviceId, testModule.Id);
            await testModuleCallbackHandler.SetModuleReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_deviceResponsePayload, ct);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                Payload = s_serviceRequestPayload,
                ResponseTimeout = s_defaultMethodResponseTimeout,
            };
            testModuleCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            Task methodReceivedTask = testModuleCallbackHandler.WaitForMethodCallbackAsync(ct);
            Task serviceSendTask = ServiceSendMethodAndVerifyResponseAsync(
                testModule.DeviceId,
                testModule.Id,
                directMethodRequest,
                s_deviceResponsePayload,
                ct);

            await Task.WhenAll(serviceSendTask, methodReceivedTask).ConfigureAwait(false);
        }

        internal class TestDateTime
        {
            public string Iso8601String { get; set; }
        }
    }
}
