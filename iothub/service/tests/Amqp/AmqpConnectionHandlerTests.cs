// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Amqp
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpConnectionHandlerTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly IIotHubServiceRetryPolicy s_RetryPolicy = new IotHubServiceNoRetry();
        private static readonly IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = s_RetryPolicy
        };
        private readonly EventHandler ConnectionLossHandler = (object sender, EventArgs e) => { };

        [TestMethod]
        public async Task AmqpConnectionHandler_SendAsync()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockAmqpConnection = new Mock<AmqpConnection>();
            var mockWorkerSession = new Mock<AmqpSessionHandler>();
            var mockAmqpCbsSessionHelper = new Mock<AmqpCbsSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                HostName,
                s_options,
                ConnectionLossHandler,
                mockAmqpCbsSessionHelper.Object,
                mockWorkerSession.Object);

            Outcome outcomeToReturn = new Accepted();

            mockWorkerSession
                .Setup(ws => ws.SendAsync(It.IsAny<AmqpMessage>(), It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(outcomeToReturn);

            // act
            Outcome act = await connectionHandler.SendAsync(amqpMessage, CancellationToken.None).ConfigureAwait(false);

            // assert
            act.Should().BeEquivalentTo(new Accepted());
        }

        [TestMethod]
        public void AmqpConnectionHandler_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockWorkerSession = new Mock<AmqpSessionHandler>();
            var mockAmqpCbsSessionHelper = new Mock<AmqpCbsSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                HostName,
                s_options,
                ConnectionLossHandler,
                mockAmqpCbsSessionHelper.Object,
                mockWorkerSession.Object);

            var ct = new CancellationToken(true);

            // act
            Func<Task> act = async () => await connectionHandler.OpenAsync(ct);

            // assert
            act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public void AmqpConnectionHandler_SendAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockWorkerSession = new Mock<AmqpSessionHandler>();
            var mockAmqpCbsSessionHelper = new Mock<AmqpCbsSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                HostName,
                s_options,
                ConnectionLossHandler,
                mockAmqpCbsSessionHelper.Object,
                mockWorkerSession.Object);

            var ct = new CancellationToken(true);

            // act
            Func<Task> act = async () => await connectionHandler.SendAsync(amqpMessage, ct);

            // assert
            act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public void AmqpConnectionHandler_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var amqpMessage = AmqpMessage.Create();

            var mockCredential = new Mock<TokenCredential>();
            var tokenCredentialProperties = new IotHubTokenCredentialProperties(HostName, mockCredential.Object);
            var mockWorkerSession = new Mock<AmqpSessionHandler>();
            var mockAmqpCbsSessionHelper = new Mock<AmqpCbsSessionHandler>();

            using var connectionHandler = new AmqpConnectionHandler(
                tokenCredentialProperties,
                IotHubTransportProtocol.Tcp,
                HostName,
                s_options,
                ConnectionLossHandler,
                mockAmqpCbsSessionHelper.Object,
                mockWorkerSession.Object);

            var ct = new CancellationToken(true);

            // act
            Func<Task> act = async () => await connectionHandler.CloseAsync(ct);

            // assert
            act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
