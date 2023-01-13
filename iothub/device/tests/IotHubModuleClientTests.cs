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
        private const string DeviceId = "module-twin-test";
        private const string ModuleId = "mongo-server";
        private const string ConnectionStringWithModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;ModuleId=mongo-server;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string ConnectionStringWithoutModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeHostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        public const string NoModuleTwinJson = "{ \"maxConnections\": 10 }";

        private DirectMethodResponse _directMethodResponseWithEmptyByteArrayPayload = new(200)
        {
            Payload = new byte[0],
        };

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionString_NullConnectionStringThrows()
        {
            Func<Task> act = async () => { await using var moduleClient = new IotHubModuleClient(null); };
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionString_WithModuleId()
        {
            await using var moduleClient = new IotHubModuleClient(ConnectionStringWithModuleId);
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionString_WithNoModuleIdThrows()
        {
            Func<Task> act = async () => { await using var moduleClient = new IotHubModuleClient(ConnectionStringWithoutModuleId); };
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionString_NoTransportSettings()
        {
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModuleClient_AuthenticationWithX509Certificate()
        {
            var auth = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, DeviceId, ModuleId);
            await using var moduleClient = new IotHubModuleClient(FakeHostName, auth, new IotHubClientOptions());
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModuleClient_AuthenticationWithX509Certificate_doesnotThrow()
        {
            var auth = new ClientAuthenticationWithX509Certificate(s_cert, s_certs, DeviceId, ModuleId);
            var creds = new IotHubConnectionCredentials(auth, FakeHostName, FakeHostName);
            await using var moduleCLient = new IotHubModuleClient(creds, new IotHubClientOptions(), null);
            moduleCLient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionStringWithClientOptions_DoesNotThrow()
        {
            // arrange
            var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = "tempModuleId"
            };

            // act
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString, clientOptions);
            
            // assert
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            await moduleClient.InnerHandler.EnableReceiveMessageAsync(It.IsAny<CancellationToken>());
            await moduleClient.SetIncomingMessageCallbackAsync((message) => Task.FromResult(MessageAcknowledgement.Complete)).ConfigureAwait(false);
            await moduleClient.InnerHandler.DisableReceiveMessageAsync(It.IsAny<CancellationToken>());


            innerHandler.Verify(
                x => x.EnableReceiveMessageAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            innerHandler.Verify(x => x.DisableReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
            innerHandler.Verify(x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void ModuleClient_ValidateIncomingMessage()
        {
            // arrange
            var testMessage = new IncomingMessage(Encoding.UTF8.GetBytes("test message"))
            {
                InputName = "endpoint1",
                MessageId = "123",
                CorrelationId = "1234",
                SequenceNumber = 123,
                To = "destination",
                UserId = "id",
                CreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                EnqueuedOnUtc = new DateTimeOffset(DateTime.MinValue),
                ExpiresOnUtc = new DateTimeOffset(DateTime.MinValue),
                MessageSchema = "schema",
                ContentType = "type",
                ContentEncoding = "encoding",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            var testMessage1 = new IncomingMessage(Encoding.UTF8.GetBytes("test message"));

            // assert
            testMessage.TryGetPayload(out bool boolPayload);
            boolPayload.Should().BeFalse();
            testMessage.TryGetPayload(out string payload);
            payload.Should().Be("test message");
            testMessage.InputName.Should().Be("endpoint1");
            testMessage.MessageId.Should().Be("123");
            testMessage.CorrelationId.Should().Be("1234");
            testMessage.SequenceNumber.Should().Be(123);
            testMessage.To.Should().Be("destination");
            testMessage.UserId.Should().Be("id");
            testMessage.CreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.EnqueuedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ExpiresOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.MessageSchema.Should().Be("schema");
            testMessage.ContentType.Should().Be("type");
            testMessage.ContentEncoding.Should().Be("encoding");
            testMessage.Properties.Should().NotBeNull();
            testMessage.PayloadConvention.Should().Be(DefaultPayloadConvention.Instance);
            testMessage1.InputName.Should().BeNull();

        }

        [TestMethod]
        public void ModuleClient_ValidateTelemetryMessage()
        {
            // arrange and act
            var testMessage = new TelemetryMessage(Encoding.UTF8.GetBytes("test message"))
            {
                InputName = "endpoint1",
                MessageId = "123",
                CorrelationId = "1234",
                UserId = "id",
                CreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                BatchCreatedOnUtc = new DateTimeOffset(DateTime.MinValue),
                EnqueuedOnUtc = new DateTimeOffset(DateTime.MinValue),
                ExpiresOnUtc = new DateTimeOffset(DateTime.MinValue),
                ComponentName = "component",
                MessageSchema = "schema",
                ContentType = "type",
                ContentEncoding = "encoding",
                PayloadConvention = DefaultPayloadConvention.Instance,
                ConnectionDeviceId = "connectionDeviceId",
                ConnectionModuleId = "connectionModuleId",
            };

            var testMessage1 = new IncomingMessage(Encoding.UTF8.GetBytes("test message"));

            // assert
            testMessage.GetPayloadObjectBytes().Should().NotBeNull();
            testMessage.InputName.Should().Be("endpoint1");
            testMessage.MessageId.Should().Be("123");
            testMessage.CorrelationId.Should().Be("1234");
            testMessage.UserId.Should().Be("id");
            testMessage.CreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.BatchCreatedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.EnqueuedOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ExpiresOnUtc.Should().Be(new DateTimeOffset(DateTime.MinValue));
            testMessage.ComponentName.Should().Be("component");
            testMessage.MessageSchema.Should().Be("schema");
            testMessage.ContentType.Should().Be("type");
            testMessage.ContentEncoding.Should().Be("encoding");
            testMessage.Properties.Should().NotBeNull();
            testMessage.PayloadConvention.Should().Be(DefaultPayloadConvention.Instance);
            testMessage.ConnectionDeviceId.Should().Be("connectionDeviceId");
            testMessage.ConnectionModuleId.Should().Be("connectionModuleId");

            testMessage1.InputName.Should().BeNull();

        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
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
        public async Task ModuleClient_SendTelemetry_ThrowsSocketException()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
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
            Func<Task> act = async () => await moduleClient.SendTelemetryAsync("output", messageWithId).ConfigureAwait(false);
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_SendTelemetry_ThrowsWebSocketException()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var innerHandler = new Mock<IDelegatingHandler>();
            innerHandler
                .Setup(x => x.SendTelemetryAsync(It.IsAny<TelemetryMessage>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromException(new WebSocketException()));
            moduleClient.InnerHandler = innerHandler.Object;

            // act
            var messageWithId = new TelemetryMessage
            {
                MessageId = messageId,
            };
            Func<Task> act = async () => await moduleClient.SendTelemetryAsync("output", messageWithId).ConfigureAwait(false);
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_Throws()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, DirectMethodRequest);
            
            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_Throws_NullException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            
            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, null);
            
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_ModuleId_Throws()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            
            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, ModuleId, DirectMethodRequest);
            
            // assert
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_ModuleId_Throws_NullException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest("TestMethodName")
            {
                PayloadConvention = DefaultPayloadConvention.Instance,
            };

            // act
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, ModuleId, null);
            
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }


        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

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
            await moduleClient.SendTelemetryAsync("output", messageWithoutId).ConfigureAwait(false);
            await moduleClient.SendTelemetryAsync("output", messageWithId).ConfigureAwait(false);
            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventBatchDoesNotSetMessageId()
        {
            // arrange
            string messageId = Guid.NewGuid().ToString();
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

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

            await moduleClient.SendTelemetryBatchAsync("output", new List<TelemetryMessage> { messageWithoutId, messageWithId }).ConfigureAwait(false);

            // assert
            messageWithoutId.MessageId.Should().BeNull();
            messageWithId.MessageId.Should().Be(messageId);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendTelemetryAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

            // act
            Func<Task> act = async () => await moduleClient.SendTelemetryAsync(new TelemetryMessage());

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendTelemetryBatchAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

            // act
            Func<Task> act = async () => await moduleClient.SendTelemetryBatchAsync(new List<TelemetryMessage> { new TelemetryMessage(), new TelemetryMessage() });

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task IotHubModuleClient_SetMethodHandlerUnsetWhenNoMethodHandler()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

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
        public async Task IotHubModuleClient_SetMethodHandlerUnsetLastMethodHandler()
        {
            // arrange
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            bool methodCallbackCalled = false;
            string actualMethodName = string.Empty;
            CustomDirectMethodPayload actualMethodBody = null;
            Task<DirectMethodResponse> methodCallback(DirectMethodRequest methodRequest)
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            }

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
