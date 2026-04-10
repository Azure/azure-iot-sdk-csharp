// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    /// <summary>
    /// Ensure that any calls to a disposed device client result in an ObjectDisposedException.
    /// </summary>
    public class DeviceClientDisposeTests
    {
        private static DeviceClient s_client;
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 60;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Create a disposed device client for the tests in this class
            var rndBytes = new byte[32];
            new Random().NextBytes(rndBytes);
            string testSharedAccessKey = Convert.ToBase64String(rndBytes);
            var csBuilder = IotHubConnectionStringBuilder.Create(
                "contoso.azure-devices.net",
                new DeviceAuthenticationWithRegistrySymmetricKey("deviceId", testSharedAccessKey));
            s_client = DeviceClient.CreateFromConnectionString(csBuilder.ToString());
            s_client.Dispose();
        }

        [TestMethod]
        public async Task DeviceClient_OpenAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.OpenAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_CloseAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CloseAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SetMethodHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetMethodHandlerAsync(
                    "methodName",
                    (request, userContext) => Task.FromResult(new MethodResponse(400)),
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SetMethodDefaultHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetMethodDefaultHandlerAsync(
                    (request, userContext) => Task.FromResult(new MethodResponse(400)),
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SetDesiredPropertyUpdateCallbackAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetDesiredPropertyUpdateCallbackAsync(
                    (desiredProperties, userContext) => TaskHelpers.CompletedTask,
                    null,
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SetReceiveMessageHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetReceiveMessageHandlerAsync(
                    (message, userContext) => TaskHelpers.CompletedTask,
                    null,
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_ReceiveAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.ReceiveAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_CompleteAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CompleteAsync("fakeLockToken", cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_AbandonAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.AbandonAsync("fakeLockToken", cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_RejectAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.RejectAsync("fakeLockToken", cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SendEventAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventAsync(msg, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_SendEventBatchAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventBatchAsync(new[] { msg }, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_GetTwinAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetTwinAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_UpdateReportedPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.UpdateReportedPropertiesAsync(new TwinCollection(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_UploadToBlobAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var stream = new MemoryStream();
#pragma warning disable CS0618 // Type or member is obsolete
            Func<Task> op = async () => await s_client.UploadToBlobAsync("blobName", stream, cts.Token).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_GetFileUploadSasUriAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetFileUploadSasUriAsync(new FileUploadSasUriRequest(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClient_CompleteFileUploadAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CompleteFileUploadAsync(new FileUploadCompletionNotification(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }
    }
}
