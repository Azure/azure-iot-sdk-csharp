// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubModuleClientTests
    {
        private static readonly string deviceId = "module-twin-test";
        private static readonly string moduleId = "mongo-server";
        private static readonly string fakeHostName = "acme.azure-devices.net";
        private static readonly string fakeGatewayHostName = "edge.iot.microsoft.com";
        private static readonly string fakeSharedAccessKey = "dGVzdFN0cmluZzQ=";
        private static readonly string connectionStringWithModuleId = $"GatewayHostName={fakeGatewayHostName};HostName={fakeHostName};DeviceId={deviceId};ModuleId={moduleId};SharedAccessKey={fakeSharedAccessKey}";
        private static readonly string connectionStringWithoutModuleId = $"GatewayHostName={fakeGatewayHostName};HostName={fakeHostName};DeviceId={deviceId};SharedAccessKey={fakeSharedAccessKey}";
        private static readonly string fakeConnectionString = $"HostName={fakeHostName};SharedAccessKeyName=AllAccessKey;DeviceId={deviceId};ModuleId={moduleId};SharedAccessKey={fakeSharedAccessKey}";
        private static readonly X509Certificate2 s_cert = new();
        private static readonly X509Certificate2Collection s_certs = new();

        public const string NoModuleTwinJson = "{ \"maxConnections\": 10 }";

        private readonly DirectMethodResponse _directMethodResponseWithEmptyByteArrayPayload = new(200)
        {
            Payload = Array.Empty<byte>(),
        };

        [TestMethod]
        public async Task IotHubModuleClient_CreateFromConnectionString_NullConnectionString_Throws()
        {
            Func<Task> act = async () => { await using var moduleClient = new IotHubModuleClient(null); };
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_CreateFromConnectionString_WithModuleId_Succeeds()
        {
            await using var moduleClient = new IotHubModuleClient(connectionStringWithModuleId);
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubModuleClient_CreateFromConnectionString_WithNoModuleId_Throws()
        {
            Func<Task> act = async () => { await using var moduleClient = new IotHubModuleClient(connectionStringWithoutModuleId); };
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_CreateFromConnectionString_NoTransportSettings_Succeeds()
        {
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubModuleClient_AuthenticationWithX509Certificate_Succeeds()
        {
            var auth = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, deviceId, moduleId);
            await using var moduleClient = new IotHubModuleClient(fakeHostName, auth, new IotHubClientOptions());
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubModuleClient_CreateFromConnectionString_WithClientOptions_Succeeds()
        {
            // arrange
            var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = "tempModuleId"
            };

            // act
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString, clientOptions);
            
            // assert
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString, options);
            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            await moduleClient.InnerHandler.EnableReceiveMessageAsync(It.IsAny<CancellationToken>());
            await moduleClient.SetIncomingMessageCallbackAsync((message) => Task.FromResult(MessageAcknowledgement.Complete)).ConfigureAwait(false);
            await moduleClient.InnerHandler.DisableReceiveMessageAsync(It.IsAny<CancellationToken>());

            // assert
            innerHandler.Verify(
                x => x.EnableReceiveMessageAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            innerHandler.Verify(x => x.DisableReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
            innerHandler.Verify(x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task IotHubModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            bool isDefaultCallbackCalled = false;
            await moduleClient
                .SetIncomingMessageCallbackAsync(
                    (message) =>
                    {
                        isDefaultCallbackCalled = true;
                        return Task.FromResult(MessageAcknowledgement.Complete);
                    })
                .ConfigureAwait(false);

            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes("test message"))
            {
                InputName = "endpoint1",
            };

            // act
            await moduleClient.OnMessageReceivedAsync(testMessage).ConfigureAwait(false);
            
            // assert
            isDefaultCallbackCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendMessagesToRoute_ThrowsSocketExceptionAsIotHubClientException()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            // This is used to simulate the transport level socket exception
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromException(new SocketException()));
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };

            // assert
            Func<Task> act = async () => await moduleClient.SendMessageToRouteAsync("output", messageWithId).ConfigureAwait(false);
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendMessagesToRoute_ThrowsWebSocketExceptionAsIotHubClientException()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            // This is used to simulate the transport level websocket exception
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromException(new WebSocketException()));
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            Func<Task> act = async () => await moduleClient.SendMessageToRouteAsync("output", messageWithId).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_InvokeMethodAsync_EdgeDevice_NullMethodRequest_Throws_NullException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            
            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(deviceId, null);
            
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_InvokeMethodAsync_EdgeModule_NullMethodRequest_Throws_NullException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(deviceId, moduleId, null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_InvokeMethodAsync_WithoutExplicitOpenAsync_Throws_InvalidOperationException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            
            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(deviceId, moduleId, DirectMethodRequest);
            
            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            await moduleClient.SendMessageToRouteAsync("output", messageWithoutId).ConfigureAwait(false);
            await moduleClient.SendMessageToRouteAsync("output", messageWithId).ConfigureAwait(false);
            
            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventBatchDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString, new IotHubClientOptions(new IotHubClientAmqpSettings()));

            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(0));
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithoutId = new TelemetryMessage();
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };

            await moduleClient.SendMessagesToRouteAsync("output", new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendTelemetryAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);

            // act
            Func<Task> act = async () => await moduleClient.SendTelemetryAsync(new TelemetryMessage());

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendTelemetryBatchAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);

            // act
            Func<Task> act = async () => await moduleClient.SendTelemetryAsync(new List<TelemetryMessage> { new TelemetryMessage(), new TelemetryMessage() });

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SetMethodHandlerUnset_WhenNoMethodHandler()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            await moduleClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            
            // assert
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SetMethodHandler_UnsetLastMethodHandler()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(fakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            };

            const string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await moduleClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await moduleClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.EnableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            methodCallbackCalled.Should().BeTrue();
            methodName.Should().Be(actualMethodName);
            methodBody.Should().BeEquivalentTo(actualMethodBody);

            // arrange
            methodCallbackCalled = false;
            await moduleClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
            directMethodRequest = new DirectMethodRequest(methodName)
            {
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(methodBody)),
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            await moduleClient.OnMethodCalledAsync(directMethodRequest).ConfigureAwait(false);

            // assert
            innerHandler.Verify(
                x => x.DisableMethodsAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            methodCallbackCalled.Should().BeFalse();
        }

        private class CustomDirectMethodPayload
        {
            [JsonProperty("grade")]
            public string Grade { get; set; }
        }
    }
}
