// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests.FileUpload
{
    [TestClass]
    [TestCategory("Unit")]
    public class FileUploadNotificationProcessorClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";
        private static readonly string s_validMockAuthenticationHeaderValue = $"SharedAccessSignature sr={HostName}&sig=thisIsFake&se=000000&skn=registryRead";

        private static IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static IotHubServiceClientOptions s_options = new IotHubServiceClientOptions
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };

        private static readonly Uri s_httpUri = new($"https://{HostName}");
        private static readonly RetryHandler s_retryHandler = new(new IotHubServiceNoRetry());

        [TestMethod]
        public async Task FileUploadNotificationProcessorClient_OpenAsync_NotSettingMessageFeedbackProcessorThrows()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.FileUploadNotifications.OpenAsync().ConfigureAwait(false);

            // assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task FileUploadNotificationProcessorClient_OpenAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                return AcknowledgementType.Abandon;
            };

            serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.FileUploadNotifications.OpenAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        
        }

        [TestMethod]
        public async Task FileUploadNotificationProcessorClient_CloseAsync_Cancelled_ThrowsOperationCanceledException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                return AcknowledgementType.Abandon;
            };

            serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
            // act
            var ct = new CancellationToken(true);
            Func<Task> act = async () => await serviceClient.FileUploadNotifications.CloseAsync(ct);

            // assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestMethod]
        public async Task FileUploadNotificationProcessorClient_OpenAsync()
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

            using var fileUploadNotificationProcessorClient = new FileUploadNotificationProcessorClient(
                HostName,
                mockCredentialProvider.Object,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);

            Func<FileUploadNotification, AcknowledgementType> OnFileUploadNotificationReceived = (fileUploadNotification) =>
            {
                return AcknowledgementType.Abandon;
            };
            var ct = new CancellationToken(false);

            fileUploadNotificationProcessorClient.FileUploadNotificationProcessor = OnFileUploadNotificationReceived;
            
            // act
            Func<Task> act = async () => await fileUploadNotificationProcessorClient.OpenAsync().ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
            mockAmqpConnectionHandler.Verify(x => x.OpenAsync(ct), Times.Once());
        }

        [TestMethod]
        public async Task FileUploadNotificationProcessorClient_CloseAsync()
        {
            // arrange
            var mockCredentialProvider = new Mock<IotHubConnectionProperties>();
            mockCredentialProvider
                .Setup(getCredential => getCredential.GetAuthorizationHeader())
                .Returns(s_validMockAuthenticationHeaderValue);


            var mockAmqpConnectionHandler = new Mock<AmqpConnectionHandler>();

            mockAmqpConnectionHandler
                .Setup(x => x.CloseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            using var fileUploadNotificationProcessorClient = new FileUploadNotificationProcessorClient(
                HostName,
                mockCredentialProvider.Object,
                s_retryHandler,
                mockAmqpConnectionHandler.Object);

            var ct = new CancellationToken(false);

            // act
            Func<Task> act = async () => await fileUploadNotificationProcessorClient.CloseAsync().ConfigureAwait(false);

            // assert
            await act.Should().NotThrowAsync().ConfigureAwait(false);
            mockAmqpConnectionHandler.Verify(x => x.CloseAsync(ct), Times.Once());
        }
    }
}
