using System;
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
        const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerOpenAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(true, token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerSendEventAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerSendEventAsyncMultipleMessagesTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerReceiveAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(new TimeSpan(0, 10, 0), token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerCompleteAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerAbandonAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("TransportHandlers")]
        public async Task AmqpTransportHandlerRejectAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
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
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
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
