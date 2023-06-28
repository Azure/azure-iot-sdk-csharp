// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
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
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            var expected = new StjCustomPayload
            {
                StringProperty = "foo",
                GuidProperty = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out StjCustomPayload actual)
                .Should().BeTrue();
            actual.StringProperty.Should().Be(expected.StringProperty);
            actual.GuidProperty.Should().Be(expected.GuidProperty);
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
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            var expected = new StjCustomPayload
            {
                StringProperty = "foo",
                GuidProperty = Guid.NewGuid().ToString(),
            };
            var properties = new ReportedProperties
            {
                { "customProperties", expected }
            };

            await deviceClient.UpdateReportedPropertiesAsync(properties, ct).ConfigureAwait(false);
            TwinProperties twin = await deviceClient.GetTwinPropertiesAsync().ConfigureAwait(false);

            // act and assert
            twin.Reported.TryGetValue("customProperties", out StjCustomPayload actual)
                .Should().BeTrue();
            actual.StringProperty.Should().Be(expected.StringProperty);
            actual.GuidProperty.Should().Be(expected.GuidProperty);
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

            string guid = Guid.NewGuid().ToString();
            string foo = "foo";

            var requestPayload = new NjCustomPayload
            {
                StringProperty = foo,
                GuidProperty = guid,
            };

            byte[] responsePayload = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(
                new StjCustomPayload
                {
                    StringProperty = foo,
                    GuidProperty = guid,
                }));

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestPayload)),
            };

            // act and assert
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient
                .SetDirectMethodCallbackAsync((msg) =>
                    {
                        msg.TryGetPayload(out StjCustomPayload actual).Should().BeTrue();
                        actual.StringProperty.Should().Be(requestPayload.StringProperty);
                        actual.GuidProperty.Should().Be(requestPayload.GuidProperty);
                        messageReceived.TrySetResult(true);
                        var response = new DirectMethodResponse(200)
                        {
                            Payload = responsePayload,
                        };
                        return Task.FromResult(response);
                    },
                    ct)
                .ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest, ct)
                .ConfigureAwait(false);
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out NjCustomPayload actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(directMethodRequest.Payload));
            await messageReceived.WaitAsync(ct).ConfigureAwait(false);
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

            var payload = new NjCustomPayload
            {
                StringProperty = "foo",
                GuidProperty = Guid.NewGuid().ToString(),
            };

            var directMethodRequest = new DirectMethodServiceRequest(MethodName)
            {
                ResponseTimeout = s_defaultMethodResponseTimeout,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)),
            };

            // act and assert
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient
                .SetDirectMethodCallbackAsync((msg) =>
                    {
                        msg.TryGetPayload(out NjCustomPayload actual).Should().BeTrue();
                        actual.StringProperty.Should().Be(payload.StringProperty);
                        actual.GuidProperty.Should().Be(payload.GuidProperty);
                        messageReceived.TrySetResult(true);
                        var response = new DirectMethodResponse(200)
                        {
                            Payload = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(payload)),
                        };
                        return Task.FromResult(response);
                    },
                    ct)
                .ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;
            DirectMethodClientResponse methodResponse = await serviceClient.DirectMethods
                .InvokeAsync(testDevice.Id, directMethodRequest, ct)
                .ConfigureAwait(false);
            methodResponse.Status.Should().Be(200);
            methodResponse.TryGetPayload(out NjCustomPayload actualClientResponsePayload).Should().BeTrue();
            JsonConvert.SerializeObject(actualClientResponsePayload).Should().BeEquivalentTo(JsonConvert.SerializeObject(directMethodRequest.Payload));
            await messageReceived.WaitAsync(ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Device_IncomingMessage_SystemTextJsonConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = SystemTextJsonPayloadConvention.Instance
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            var payload = new NjCustomPayload
            {
                StringProperty = "foo",
                GuidProperty = Guid.NewGuid().ToString(),
            };

            // act and assert
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient
                .SetIncomingMessageCallbackAsync((msg) =>
                    {
                        msg.TryGetPayload(out StjCustomPayload actual).Should().BeTrue();
                        actual.StringProperty.Should().Be(payload.StringProperty);
                        actual.GuidProperty.Should().Be(payload.GuidProperty);
                        messageReceived.TrySetResult(true);
                        return Task.FromResult(MessageAcknowledgement.Complete);
                    },
                    ct)
                .ConfigureAwait(false);
            var svcMsg = new OutgoingMessage(payload);
            await serviceClient.Messages.SendAsync(testDevice.Id, svcMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={svcMsg.MessageId} - to be received on callback");

            await messageReceived.WaitAsync(ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Device_IncomingMessage_DefaultPayloadConvention_Payload()
        {
            // arrange
            using var cts = new CancellationTokenSource(s_testTimeout);
            CancellationToken ct = cts.Token;
            await using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(s_devicePrefix, ct: ct).ConfigureAwait(false);
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                PayloadConvention = DefaultPayloadConvention.Instance
            };
            IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(options);
            await testDevice.OpenWithRetryAsync(ct).ConfigureAwait(false);

            IotHubServiceClient serviceClient = TestDevice.ServiceClient;

            await serviceClient.Messages.OpenAsync(ct).ConfigureAwait(false);

            // Now, send a message to the device from the service.
            var payload = new NjCustomPayload
            {
                StringProperty = "foo",
                GuidProperty = Guid.NewGuid().ToString(),
            };

            // act and assert
            var messageReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await deviceClient
                .SetIncomingMessageCallbackAsync((msg) =>
                    {
                        msg.TryGetPayload(out NjCustomPayload actual).Should().BeTrue();
                        actual.StringProperty.Should().Be(payload.StringProperty);
                        actual.GuidProperty.Should().Be(payload.GuidProperty);
                        messageReceived.TrySetResult(true);
                        return Task.FromResult(MessageAcknowledgement.Complete);
                    },
                    ct)
                .ConfigureAwait(false);
            var svcMsg = new OutgoingMessage(payload);
            await serviceClient.Messages.SendAsync(testDevice.Id, svcMsg, ct).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"Sent C2D message from service, messageId={svcMsg.MessageId} - to be received on callback");

            await messageReceived.WaitAsync(ct).ConfigureAwait(false);
        }
    }
}
