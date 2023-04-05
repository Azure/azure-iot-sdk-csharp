// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    /// <summary>
    /// Ensure that any calls to a disposed device client result in an ObjectDisposedException.
    /// </summary>
    public class IotHubDeviceClientDisposeTests
    {
        private static IotHubDeviceClient s_client;

        [ClassInitialize]
        public static async Task ClassInitializeAsync(TestContext context)
        {
            // Create a disposed device client for the tests in this class
            var rndBytes = new byte[32];
            new Random().NextBytes(rndBytes);
            string testSharedAccessKey = Convert.ToBase64String(rndBytes);
            var csBuilder = new IotHubConnectionString(
                "contoso.azure-devices.net",
                null,
                "deviceId",
                null,
                null,
                testSharedAccessKey,
                null);
            s_client = new IotHubDeviceClient(csBuilder.ToString(), new IotHubClientOptions(new IotHubClientAmqpSettings()));
            await s_client.DisposeAsync();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_OpenAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.OpenAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CloseAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CloseAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDirectMethodCallbackAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetDirectMethodCallbackAsync(
                    (request) => Task.FromResult(new DirectMethodResponse(400)),
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetDesiredPropertyUpdateCallbackAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetDesiredPropertyUpdateCallbackAsync(
                    (desiredProperties) => Task.CompletedTask,
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SetIncomingMessageHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetIncomingMessageCallbackAsync(
                    (message) => Task.FromResult(MessageAcknowledgement.Complete),
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var msg = new TelemetryMessage();
            Func<Task> op = async () => await s_client.SendTelemetryAsync(msg, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_SendTelemetryAsync_Batch_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var msg = new TelemetryMessage();
            Func<Task> op = async () => await s_client.SendTelemetryAsync(new[] { msg }, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetTwinPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetTwinPropertiesAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_UpdateReportedPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.UpdateReportedPropertiesAsync(new ReportedProperties(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_GetFileUploadSasUriAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetFileUploadSasUriAsync(new FileUploadSasUriRequest("blobName"), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubDeviceClient_CompleteFileUploadAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CompleteFileUploadAsync(new FileUploadCompletionNotification("correlationId", true), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }
    }
}