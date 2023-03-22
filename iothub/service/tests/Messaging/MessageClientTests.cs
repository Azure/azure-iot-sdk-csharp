// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Messaging
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";
        private static readonly Uri s_httpUri = new($"https://{HostName}");

        private static IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };
        private readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task MessagesClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.Messages.OpenAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task MessagesClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.Messages.CloseAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task MessagesClient_SendAsync_WithModule_NullDeviceIdThrows()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();

            var msg = new OutgoingMessage(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync(null, "moduleId123", msg);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessagesClient_SendAsync_WithModule_NullModuleIdThrows()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();

            var msg = new OutgoingMessage(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync("deviceId123", null, msg);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        [DataRow(null, "moduleId123")]
        [DataRow("deviceId123", null)]
        public async Task MessagesClient_SendAsync_NullParamsThrows(string deviceId, string moduleId)
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var msg = new OutgoingMessage(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync(deviceId, moduleId, msg);
            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        [DataRow(" ", "moduleId123")]
        [DataRow("deviceId123", " ")]
        [DataRow("", "moduleId123")]
        [DataRow("deviceId123", "")]
        public async Task MessagesClient_SendAsync_EmptyAndSpaceInParamsThrows(string deviceId, string moduleId)
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new OutgoingMessage(payloadBytes);

            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync(deviceId, moduleId, msg);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task MessageClient_SendAsync_WithoutExplicitOpenAsync_ThrowsInvalidOperationException()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new OutgoingMessage(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync("deviceId123", msg);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task MessageClient_OpenAsync()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);

            var mockAmqpConnectionHandler = new Mock<AmqpConnectionHandler>();

            mockAmqpConnectionHandler
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var messagesClient = new MessagesClient(
                HostName,
                mockCredentialProvider.Object,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);
            var ct = new CancellationToken(false);

            // act
            Func<Task> act = async () => await messagesClient.OpenAsync().ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
            mockAmqpConnectionHandler.Verify(x => x.OpenAsync(ct), Times.Once());
        }

        [TestMethod]
        public async Task MessageClient_PurgeMessageQueueAsync_NullDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.PurgeMessageQueueAsync(null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessageClient_PurgeMessageQueueAsync_EmptyDeviceIdThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.PurgeMessageQueueAsync(string.Empty);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task MessageClient_PurgeMessage()
        {
            // arrange
            string deviceId = "deviceId123";
            int totalMessagesPurged = 1;

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);

            var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

            var purgeMessageQueueResultToReturn = new PurgeMessageQueueResult
            {
                DeviceId = deviceId,
                TotalMessagesPurged = totalMessagesPurged
            };

            using var mockHttpResponse = new HttpResponseMessage
            {
                Content = HttpMessageHelper.SerializePayload(purgeMessageQueueResultToReturn),
                StatusCode = HttpStatusCode.OK,
            };

            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClient
                .Setup(restOp => restOp.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpResponse);

            using var messageClient = new MessagesClient(
                HostName,
                mockCredentialProvider.Object,
                mockHttpClient.Object,
                mockHttpRequestFactory,
                s_options,
                s_retryHandler);

            // act
            PurgeMessageQueueResult result = await messageClient.PurgeMessageQueueAsync(deviceId);

            // assert
            result.DeviceId.Should().Be(deviceId);
            result.TotalMessagesPurged.Should().Be(totalMessagesPurged);
        }

        [TestMethod]
        public async Task MessageClient_SendAsync()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new OutgoingMessage(payloadBytes);

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);

            var mockAmqpConnectionHandler = new Mock<AmqpConnectionHandler>();

            mockAmqpConnectionHandler
                .Setup(x => x.IsOpen)
                .Returns(true);

            Outcome outcomeToReturn = new Accepted();

            mockAmqpConnectionHandler
                .Setup(x => x.SendAsync(It.IsAny<AmqpMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(outcomeToReturn));

            using var messagesClient = new MessagesClient(
                HostName,
                mockCredentialProvider.Object,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);

            Func<Task> act = async () => await messagesClient.SendAsync("deviceId123", msg).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MessageClient_SendAsync_DescriptiorCodeNotAcceptedThrows()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new OutgoingMessage(payloadBytes);

            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);

            var mockAmqpConnectionHandler = new Mock<AmqpConnectionHandler>();

            mockAmqpConnectionHandler
                .Setup(x => x.OpenAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockAmqpConnectionHandler
                .Setup(x => x.IsOpen)
                .Returns(true);

            Outcome outcomeToReturn = new Rejected();

            mockAmqpConnectionHandler
                .Setup(x => x.SendAsync(It.IsAny<AmqpMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(outcomeToReturn));

            using var messagesClient = new MessagesClient(
                HostName,
                mockCredentialProvider.Object,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);

            Func<Task> act = async () => await messagesClient.SendAsync("deviceId123", msg).ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>().ConfigureAwait(false);
        }
    }
}
