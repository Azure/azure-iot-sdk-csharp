// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Feedback
{
    [TestClass]
    [TestCategory("Unit")]
    public class MessageFeedbackProcessorClientTests
    {
        private const string HostName = "contoso.azure-devices.net";
        private static readonly string s_connectionString = $"HostName={HostName};SharedAccessKeyName=iothubowner;SharedAccessKey=dGVzdFN0cmluZzE=";

        private static IIotHubServiceRetryPolicy noRetryPolicy = new IotHubServiceNoRetry();
        private static IotHubServiceClientOptions s_options = new IotHubServiceClientOptions
        {
            Protocol = IotHubTransportProtocol.Tcp,
            RetryPolicy = noRetryPolicy
        };

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
