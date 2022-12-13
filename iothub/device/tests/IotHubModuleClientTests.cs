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
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubModuleClientTests
    {
        private const string ConnectionStringWithModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;ModuleId=mongo-server;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string ConnectionStringWithoutModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeHostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        public const string NoModuleTwinJson = "{ \"maxConnections\": 10 }";

        private DirectMethodResponse _directMethodResponseWithPayload = new(200)
        {
            Payload = 123,
        };

        private DirectMethodResponse _directMethodResponseWithEmptyByteArrayPayload = new(200)
        {
            Payload = new byte[0]
        };

        public readonly string ValidDeviceTwinJson = string.Format(
            @"
{{
    ""{1}"": {{
        ""properties"": {{
            ""desired"": {{
                ""maxConnections"": 10,
                ""$metadata"": {{
                    ""$lastUpdated"": ""2017-05-30T22:37:31.1441889Z"",
                    ""$lastUpdatedVersion"": 2
                }}
            }}
        }}
    }},
    ""nginx-server"": {{
        ""properties"": {{
            ""desired"": {{
                ""forwardUrl"": ""http://example.com""
            }}
        }}
    }}
}}
",
            DeviceId,
            ModuleId);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ModuleClient_CreateFromConnectionString_NullConnectionStringThrows()
        {
            await using var moduleClient = new IotHubModuleClient(null);
        }

        [TestMethod]
        public async Task ModuleClient_CreateFromConnectionString_WithModuleId()
        {
            await using var moduleClient = new IotHubModuleClient(ConnectionStringWithModuleId);
            moduleClient.Should().NotBeNull();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ModuleClient_CreateFromConnectionString_WithNoModuleIdThrows()
        {
            await using var moduleClient = new IotHubModuleClient(ConnectionStringWithoutModuleId);
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
            // setup
            var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = "tempModuleId"
            };

            // act
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString, clientOptions);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            await moduleClient.SetIncomingMessageCallbackAsync((message) => Task.FromResult(MessageAcknowledgement.Complete)).ConfigureAwait(false);

            innerHandler.Verify(
                x => x.EnableReceiveMessageAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            innerHandler.Verify(x => x.DisableReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Never);
            innerHandler.Verify(x => x.SendMethodResponseAsync(It.IsAny<DirectMethodResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
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

            await moduleClient.OnMessageReceivedAsync(testMessage).ConfigureAwait(false);
            Assert.IsTrue(isDefaultCallbackCalled);
        }

        [TestMethod]
        [ExpectedException(typeof(IotHubClientException))]
        public async Task ModuleClient_SendTelemetry_ThrowsSocketException()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
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
            await moduleClient.SendTelemetryAsync("output", messageWithId).ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(IotHubClientException))]
        public async Task ModuleClient_SendTelemetry_ThrowsWebSocketException()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
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
            await moduleClient.SendTelemetryAsync("output", messageWithId).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_Throws()
        {
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, DirectMethodRequest);
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_Throws_NullException()
        {
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, null);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_ModuleId_Throws()
        {
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, ModuleId, DirectMethodRequest);
            await act.Should().ThrowAsync<IotHubClientException>();
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethod_ModuleId_Throws_NullException()
        {
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            var DirectMethodRequest = new DirectMethodRequest
            {
                MethodName = "TestMethodName",
                PayloadConvention = DefaultPayloadConvention.Instance,
            };
            Func<Task> act = async () => await moduleClient.InvokeMethodAsync(DeviceId, ModuleId, null);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }


        [TestMethod]
        public async Task MessageIdDefaultNotSet_SendEventDoesNotSetMessageId()
        {
            // arrange
            var messageId = Guid.NewGuid().ToString();
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
            var messageId = Guid.NewGuid().ToString();
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
            await using var moduleClient = new IotHubModuleClient(FakeConnectionString);

            var innerHandler = new Mock<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler.Object;

            await moduleClient.SetDirectMethodCallbackAsync(null).ConfigureAwait(false);
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
            Func<DirectMethodRequest, Task<DirectMethodResponse>> methodCallback = (methodRequest) =>
            {
                actualMethodName = methodRequest.MethodName;
                bool methodReceived = methodRequest.TryGetPayload(out actualMethodBody);
                methodCallbackCalled = true;
                return Task.FromResult(_directMethodResponseWithEmptyByteArrayPayload);
            };

            string methodName = "TestMethodName";
            var methodBody = new CustomDirectMethodPayload { Grade = "good" };
            await moduleClient.SetDirectMethodCallbackAsync(methodCallback).ConfigureAwait(false);
            var directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
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
            directMethodRequest = new DirectMethodRequest
            {
                MethodName = methodName,
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

        //[TestMethod]
        //public void ModuleClient_Dispose()
        //{

        //}

        private class CustomDirectMethodPayload
        {
            [JsonProperty("grade")]
            public string Grade { get; set; }
        }
    }

}
