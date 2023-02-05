// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Transport;
using Microsoft.Azure.Devices.Amqp;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpConnectionHandlerTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private const string LinkAddress = "contoso.azure-devices.net";
        private static readonly IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static readonly IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };
        private static EventHandler ConnectionLossHandler = (object sender, EventArgs e) => { };


        [TestMethod]
        public void AmqpConnectionHandler_OpenAsync()
        {
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockAmqpConnection = new Mock<AmqpConnection>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                LinkAddress,
                s_options,
                ConnectionLossHandler);

            var ct = new CancellationToken();

            var mockAmqpTransportInitiator = new Mock<TransportInitiator>();
        }

        [TestMethod]
        public void AmqpConnectionHandler_SendAsync()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var message = new Message(payloadBytes);

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockAmqpConnection = new Mock<AmqpConnection>();
            var mockWorkerSession = new Mock<AmqpSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                LinkAddress,
                s_options,
                ConnectionLossHandler,
                mockWorkerSession.Object);

            Outcome outcomeToReturn = new Accepted();

            mockWorkerSession
                .Setup(ws => ws.SendAsync(It.IsAny<AmqpMessage>(), It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(outcomeToReturn);

            var ct = new CancellationToken();

            // act
            Func<Task> act = async () => await connectionHandler.SendAsync(amqpMessage, ct);

            // assert
            act.Should().NotThrowAsync().ConfigureAwait(false);
        }

        [TestMethod]
        public void AmqpConnectionHandler_SendAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var message = new Message(payloadBytes);

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockWorkerSession = new Mock<AmqpSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                LinkAddress,
                s_options,
                ConnectionLossHandler,
                mockWorkerSession.Object);

            var ct = new CancellationToken(true);

            // act
            Func<Task> act = async () => await connectionHandler.SendAsync(amqpMessage, ct);

            // assert
            act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public void AmqpConnectionHandler_CloseAsync()
        {
            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockAmqpConnection = new Mock<AmqpConnection>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                LinkAddress,
                s_options,
                ConnectionLossHandler);

            var ct = new CancellationToken();

            var mockAmqpTransportInitiator = new Mock<AmqpTransportInitiator>();


            //mockAmqpTransportInitiator.Setup(i => i.ConnectAsync(It.IsAny<CancellationToken>())).Returns();
        }
    }
}
