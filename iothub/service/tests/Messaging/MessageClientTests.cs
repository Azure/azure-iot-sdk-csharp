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

        [TestMethod]
        public async Task MessageClient_SendAsync_WithoutExplicitOpenAsync_ThrowsIotHubServiceException()
        {
            // arrange
            string payloadString = "Hello, World!";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadString);
            var msg = new Message(payloadBytes);

            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.SendAsync("deviceId123", msg);

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
        }

        [TestMethod]
        public async Task MessageClient_PurgeMessageQueueAsync_WithoutExplicitOpenAsync_ThrowsIotHubServiceException()
        {
            // arrange
            using var serviceClient = new IotHubServiceClient(
                s_connectionString,
                s_options);

            // act
            Func<Task> act = async () => await serviceClient.Messages.PurgeMessageQueueAsync("deviceId123");

            // assert
            await act.Should().ThrowAsync<IotHubServiceException>();
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
            Func<Task> act = async () => await serviceClient.Messages.PurgeMessageQueueAsync(String.Empty);

            // assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {

        }
    }
}
