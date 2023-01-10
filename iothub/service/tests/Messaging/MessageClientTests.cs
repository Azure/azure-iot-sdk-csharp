// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
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
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        private static Uri s_httpUri = new($"https://{HostName}");
        private static IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static IotHubServiceClientOptions s_options = new IotHubServiceClientOptions
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };
        private static RetryHandler s_retryHandler = new(noRetryPolicy);


        //[TestMethod]
        //arrange
        //public async Task MessagesClient_OpenAsync()
        //{
        //    arrange
        //    using var serviceClient = new IotHubServiceClient(
        //        s_connectionString,
        //        s_options);

        //    act
        //   var ct = new CancellationToken(false);
        //    Func<Task> act = async () => await serviceClient.Messages.OpenAsync(ct);

        //    assert
        //   await act.Should().NotThrowAsync().ConfigureAwait(false);
        //}

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
        public async Task MessagesClient_SendAsync_NullDeviceIdThrows()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            var msg = new Message(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync(null, msg);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessagesClient_SendAsync_NullMessageThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync("deviceId", (Message)null);

            // assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task MessagesClient_SendAsync_EmptyDeviceIdThrows()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new Message(payloadBytes);

            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync(String.Empty, msg);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        //[TestMethod]
        //public async Task MessagesClient_SendAsync()
        //{
        //    arrange
        //    string payloadString = "Hello, World!";
        //    byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
        //    var msg = new Message(payloadBytes);

        //    var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
        //    mockCredentialProvider
        //        .Setup(getCredential => getCredential.GetAuthorizationHeader())
        //        .Returns(s_validMockAuthenticationHeaderValue);

        //    var mockHttpRequestFactory = new HttpRequestMessageFactory(s_httpUri, "");

        //    AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(msg);
        //    AmqpSymbol amqpSymbol = new AmqpSymbol("symbol123");
        //    var mockOutcome = new Outcome(amqpSymbol, 36uL);
        //    var mockAmqpConnectionHandler = new Mock<AmqpConnectionHandler>();

        //    mockAmqpConnectionHandler
        //        .Setup(op => op.SendAsync(amqpMessage, It.IsAny<CancellationToken>()))
        //        .ReturnsAsync(mockOutcome);

        //    var mockHttpClient = new Mock<HttpClient>();
        //    using var messagesClient = new MessagesClient(
        //        HostName,
        //        mockCredentialProvider.Object,
        //        mockHttpClient.Object,
        //        mockHttpRequestFactory,
        //        s_options,
        //        s_retryHandler);

        //    act
        //    Func<Task> act = async () => await messagesClient.SendAsync("deviceId123", "moduleId123", msg);

        //    assert
        //   await act.Should().NotThrowAsync().ConfigureAwait(false);
        //}
    }
}
