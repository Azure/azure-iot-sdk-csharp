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
    /// Ensure that any calls to a disposed module client result in an ObjectDisposedException.
    /// </summary>
    public class ModuleClientDisposeTests
    {
        private static ModuleClient s_client;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Create a disposed device client for the tests in this class
            var rndBytes = new byte[32];
            new Random().NextBytes(rndBytes);
            string testSharedAccessKey = Convert.ToBase64String(rndBytes);
            var csBuilder = IotHubConnectionStringBuilder.Create(
                "contoso.azure-devices.net",
                new ModuleAuthenticationWithRegistrySymmetricKey("deviceId","moduleId", testSharedAccessKey));
            s_client = ModuleClient.CreateFromConnectionString(csBuilder.ToString());
            s_client.Dispose();
        }

        [TestMethod]
        public async Task ModuleClient_OpenAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.OpenAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_CloseAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CloseAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetMethodHandlerAsync_ThrowsWhenClientIsDisposed()
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
        public async Task ModuleClient_SetMethodDefaultHandlerAsync_ThrowsWhenClientIsDisposed()
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
        public async Task ModuleClient_SetDesiredPropertyUpdateCallbackAsync_ThrowsWhenClientIsDisposed()
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
        public async Task DeviceClient_SetMessageHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetMessageHandlerAsync(
                    (message, userContext) => Task.FromResult(MessageResponse.Completed),
                    null,
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetInputMessageHandlerAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client
                .SetInputMessageHandlerAsync(
                    "input",
                    (message, userContext) => Task.FromResult(MessageResponse.Completed),
                    null,
                    cts.Token)
                .ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_CompleteAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CompleteAsync("fakeLockToken", cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_AbandonAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.AbandonAsync("fakeLockToken", cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SendEventAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventAsync(msg, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SendEventAsync_ToOutput_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventAsync("output", msg, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SendEventBatchAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventBatchAsync(new[] { msg }, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SendEventBatchAsync_ToOutput_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var msg = new Message();
            Func<Task> op = async () => await s_client.SendEventBatchAsync("output", new[] { msg }, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_GetTwinAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetTwinAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_UpdateReportedPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.UpdateReportedPropertiesAsync(new TwinCollection(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethodAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.InvokeMethodAsync("deviceId", new MethodRequest("name"), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethodAsync_ToModule_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.InvokeMethodAsync("deviceId", "moduleId", new MethodRequest("name"), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }
    }
}
