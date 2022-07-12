// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Devices.Client.Test.ConnectionString;
    using Microsoft.Azure.Devices.Client.Transport;

    [TestClass]
    [TestCategory("Unit")]
    public class HttpTransportHandlerTests
    {
        const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task HttpTransportHandler_SendEventAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpTransportHandler_SendEventAsync_MultipleMessages_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpTransportHandler_ReceiveAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveMessageAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpTransportHandler_CompleteAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpTransportHandler_AbandonAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HttpTransportHandler_RejectAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public void HttpTransportHandler_EnableReceiveMessageAsync_ThrowsNotSupportedException()
        {
            // arrange
            using var tokenSource = new CancellationTokenSource();
            HttpTransportHandler mockTransport = CreateFromConnectionString();

            // act
            Func<Task> act = async () =>
            {
                await mockTransport.EnableReceiveMessageAsync(tokenSource.Token).ConfigureAwait(false);
            };

            // assert
            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void HttpTransportHandler_DisableReceiveMessageAsync_ThrowsNotSupportedException()
        {
            // arrange
            using var tokenSource = new CancellationTokenSource();
            HttpTransportHandler mockTransport = CreateFromConnectionString();

            // act
            Func<Task> act = async () =>
            {
                await mockTransport.DisableReceiveMessageAsync(tokenSource.Token).ConfigureAwait(false);
            };

            // assert
            act.Should().Throw<NotSupportedException>();
        }

        HttpTransportHandler CreateFromConnectionString()
        {
            return new HttpTransportHandler(new PipelineContext(), IotHubConnectionStringExtensions.Parse(DumpyConnectionString), new Http1TransportSettings());
        }

        private async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
                Assert.Fail("Fail to skip execution of this operation using cancellation token.");
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
