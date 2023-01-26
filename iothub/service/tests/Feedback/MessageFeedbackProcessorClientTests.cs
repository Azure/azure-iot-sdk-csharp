// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.Feedback
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageFeedbackProcessorClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static IotHubServiceClientOptions s_options = new()
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task MessageFeedbackProcessorClient_OpenAsync_NotSettingMessageFeedbackProcessorThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);
            
            // act
            Func<Task> act = async () => await serviceClient.MessageFeedback.OpenAsync().ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task MessageFeedbackProcessorClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.MessageFeedback.OpenAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task MessageFeedbackProcessorClient_OpenAsync_Ok()
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

            using var messageFeedbackProcessorClient = new MessageFeedbackProcessorClient(
                HostName,
                mockCredentialProvider.Object,
                s_options,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);

            AcknowledgementType messageFeedbackProcessor(FeedbackBatch FeedbackBatch) => AcknowledgementType.Complete;

            messageFeedbackProcessorClient.MessageFeedbackProcessor = messageFeedbackProcessor;

            var ct = new CancellationToken(false);

            // act
            Func<Task> act = async () => await messageFeedbackProcessorClient.OpenAsync(ct).ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
            mockAmqpConnectionHandler.Verify(x => x.OpenAsync(ct), Times.Once());
        }

        [TestMethod]
        public async Task MessageFeedbackProcessorClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.MessageFeedback.CloseAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
