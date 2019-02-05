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
    public class AmqpTransportHandlerTests
    {
        const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_OpenAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(true, token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_SendEventAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_SendEventAsync_MultipleMessages_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_ReceiveAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(new TimeSpan(0, 10, 0), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_CompleteAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_AbandonAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token));
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandler_RejectAsync_TokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token));
        }

        // Uncomment later once we support throwing exceptions on TransportSettingsChange
        //[TestMethod]
        //[TestCategory("TransportHandlers")]
        //public void AmqpTransportHandler_RejectAmqpSettingsChange()
        //{
        //    var amqpTransportHandler1 = new AmqpTransportHandler(new PipelineContext(), IotHubConnectionString.Parse(DumpyConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
        //    {
        //        Pooling = true,
        //        MaxPoolSize = 10,
        //        ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        //    }));

        //    try
        //    {
        //        // Try to create a set AmqpTransportHandler with different connection pool settings.
        //        var amqpTransportHandler2 = new AmqpTransportHandler(new PipelineContext(), IotHubConnectionString.Parse(DumpyConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
        //        {
        //            Pooling = true,
        //            MaxPoolSize = 7, // different pool size
        //            ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        //        }));
        //    }
        //    catch (ArgumentException ae)
        //    {
        //        Assert.IsTrue(ae.Message.Contains("AmqpTransportSettings cannot be modified from the initial settings."), "Did not return the correct error message");
        //    }
        //}

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

        AmqpTransportHandler CreateFromConnectionString()
        {
            return new AmqpTransportHandler(
                new PipelineContext(), 
                IotHubConnectionStringExtensions.Parse(DumpyConnectionString), 
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only), 
                (o, ea) => { }, 
                (o, ea) => { return TaskHelpers.CompletedTask; });
        }
    }
}
