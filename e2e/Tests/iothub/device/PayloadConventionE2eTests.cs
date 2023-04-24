// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.E2ETests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Twins
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub-Client")]
    public class PayloadConventionE2eTest : E2EMsTestBase
    {
        private static readonly string s_devicePrefix = $"{nameof(PayloadConventionE2eTest)}_";
        private const string MethodName = "MethodE2ETest";
        private static readonly DirectMethodRequestPayload s_customTypeRequest = new() { DesiredState = "on" };
        private static readonly DirectMethodResponsePayload s_customTypeResponse = new() { CurrentState = "off" };
        private static readonly TimeSpan s_defaultMethodResponseTimeout = TimeSpan.FromSeconds(30);

        [TestMethod]
        public async Task Device_Twin_SystemTextJsonConvention_UpdateReportedProperties()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            PayloadConvention convention = SystemTextJsonPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            var expected = new DeviceCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out DeviceCustomProperty actual)
                .Should().BeTrue();
            actual.CustomProperty.Should().Be(expected.CustomProperty);
            actual.Guid.Should().Be(expected.Guid);
        }

        [TestMethod]
        public async Task Device_Twin_DefaultPayloadConvention_UpdateReportedProperties()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            PayloadConvention convention = DefaultPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            await using var deviceClient = new IotHubDeviceClient(testDevice.ConnectionString, options);
            await deviceClient.OpenAsync().ConfigureAwait(false);

            var expected = new DeviceCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out DeviceCustomProperty actual)
                .Should().BeTrue();
            actual.CustomProperty.Should().Be(expected.CustomProperty);
            actual.Guid.Should().Be(expected.Guid);
        }

        [TestMethod]
        public async Task Device_DirectMethod_SystemTextJsonConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            PayloadConvention convention = SystemTextJsonPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_customTypeResponse, ct);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
                Payload = s_customTypeRequest,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            // act and assert
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest, ct)
                .ConfigureAwait(false);
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out DirectMethodResponsePayload actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(s_customTypeResponse));
        }

        [TestMethod]
        public async Task Device_DirectMethod_DefaultPayloadConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            PayloadConvention convention = DefaultPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            using var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);
            await testDeviceCallbackHandler.SetDeviceReceiveMethodAndRespondAsync<DirectMethodRequestPayload>(s_customTypeResponse, ct);

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
                Payload = s_customTypeRequest,
            };
            testDeviceCallbackHandler.ExpectedDirectMethodRequest = directMethodRequest;

            // act and assert
            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest, ct)
                .ConfigureAwait(false);
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out DirectMethodResponsePayload actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(s_customTypeResponse));
        }

        [TestMethod]
        public async Task Device_IncomingMessage_SystemTextJsonConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            PayloadConvention convention = SystemTextJsonPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<DeviceCustomProperty>(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            var payload = new DeviceCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            OutgoingMessage firstMsg = new OutgoingMessage(payload);
            //OutgoingMessage firstMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = firstMsg;
            await serviceClient.Messages.SendAsync(testDevice.Id, firstMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={firstMsg.MessageId} - to be received on callback");

            await deviceHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Device_IncomingMessage_DefaultPayloadConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix).ConfigureAwait(false);
            PayloadConvention convention = DefaultPayloadConvention.Instance;
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = convention
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);
            using var deviceHandler = new TestDeviceCallbackHandler(deviceClient, testDevice.Id);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, set a callback on the device client to receive C2D messages.
            await deviceHandler.SetIncomingMessageCallbackHandlerAndCompleteMessageAsync<DeviceCustomProperty>(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            var payload = new DeviceCustomProperty
            {
                CustomProperty = "foo",
                Guid = Guid.NewGuid().ToString(),
            };
            OutgoingMessage firstMsg = new OutgoingMessage(payload);
            //OutgoingMessage firstMsg = OutgoingMessageHelper.ComposeTestMessage(out string _, out string _);
            deviceHandler.ExpectedOutgoingMessage = firstMsg;
            await serviceClient.Messages.SendAsync(testDevice.Id, firstMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={firstMsg.MessageId} - to be received on callback");

            await deviceHandler.WaitForIncomingMessageCallbackAsync(ct).ConfigureAwait(false);
        }
    }
}
