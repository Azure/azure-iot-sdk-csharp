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
    public class IotHubModuleClientDisposeTests
    {
        private static IotHubModuleClient s_client;

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
                "moduleId",
                null,
                testSharedAccessKey,
                null);
            s_client = new IotHubModuleClient(csBuilder.ToString(), new IotHubClientOptions(new IotHubClientAmqpSettings()));
            await s_client.DisposeAsync();
        }

        [TestMethod]
        public async Task IotHubModuleClient_OpenAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.OpenAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_CloseAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.CloseAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SetDirectMethodCallbackAsync_ThrowsWhenClientIsDisposed()
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
        public async Task IotHubModuleClient_SetDesiredPropertyUpdateCallbackAsync_ThrowsWhenClientIsDisposed()
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
        public async Task IotHubModuleClient_SetIncomingMessageHandlerAsync_ThrowsWhenClientIsDisposed()
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
        public async Task IotHubModuleClient_SendTelemetryAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var msg = new TelemetryMessage();
            Func<Task> op = async () => await s_client.SendTelemetryAsync(msg, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendTelemetryAsync_Batch_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var msg = new TelemetryMessage();
            Func<Task> op = async () => await s_client.SendTelemetryAsync(new[] { msg }, cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_GetTwinPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.GetTwinPropertiesAsync(cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_UpdateReportedPropertiesAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.UpdateReportedPropertiesAsync(new ReportedProperties(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendMessageToRouteAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.SendMessageToRouteAsync("output1", new TelemetryMessage(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_SendMessagesToRouteAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.SendMessagesToRouteAsync("output1", new[] { new TelemetryMessage() } , cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_InvokeMethodAsync_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.InvokeMethodAsync("deviceId", new DirectMethodRequest(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task IotHubModuleClient_InvokeMethodAsync_ToModule_ThrowsWhenClientIsDisposed()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            Func<Task> op = async () => await s_client.InvokeMethodAsync("deviceId", "moduleId", new DirectMethodRequest(), cts.Token).ConfigureAwait(false);
            await op.Should().ThrowAsync<ObjectDisposedException>().ConfigureAwait(false);
        }
    }
}