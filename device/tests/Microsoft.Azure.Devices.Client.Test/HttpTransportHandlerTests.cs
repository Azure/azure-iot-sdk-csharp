﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Client.Test.ConnectionString;

    [TestClass]
    public class HttpTransportHandlerTests
    {
        const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_SendEventAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_SendEventAsync_MultipleMessages_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_ReceiveAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(new TimeSpan(0, 0, 0), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_CompleteAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_AbandonAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task HttpTransportHandler_RejectAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token));
        }

        HttpTransportHandler CreateFromConnectionString()
        {
            return new HttpTransportHandler(new PipelineContext(), IotHubConnectionStringExtensions.Parse(DumpyConnectionString), new Http1TransportSettings());
        }

        async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token);
            }
            catch (SocketException)
            {
                Assert.Fail("Fail to skip execution of this operation using cancellation token.");
            }
        }
    }
}
