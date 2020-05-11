// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Test.ConnectionString;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Transport
{
    [TestClass]
    [TestCategory("Unit")]
    public class AmqpTransportHandlerTests
    {
        private const string DumpyConnectionString = "HostName=Do.Not.Exist;SharedAccessKeyName=AllAccessKey;DeviceId=FakeDevice;SharedAccessKey=dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task AmqpTransportHandlerOpenAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().OpenAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerSendEventAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new Message(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerSendEventAsyncMultipleMessagesTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().SendEventAsync(new List<Message>(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerReceiveAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().ReceiveAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerCompleteAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().CompleteAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerAbandonAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AbandonAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerRejectAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectAsync(Guid.NewGuid().ToString(), token)).ConfigureAwait(false);
        }

        [Ignore] // TODO #584 Uncomment later once we support throwing exceptions on TransportSettingsChange
        [TestMethod]
        public void AmqpTransportHandler_RejectAmqpSettingsChange()
        {
            //var amqpTransportHandler1 = new AmqpTransportHandler(new PipelineContext(), IotHubConnectionString.Parse(DumpyConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
            //{
            //    Pooling = true,
            //    MaxPoolSize = 10,
            //    ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            //}));
            //
            //try
            //{
            //    // Try to create a set AmqpTransportHandler with different connection pool settings.
            //    var amqpTransportHandler2 = new AmqpTransportHandler(new PipelineContext(), IotHubConnectionString.Parse(DumpyConnectionString), new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 60, new AmqpConnectionPoolSettings()
            //    {
            //        Pooling = true,
            //        MaxPoolSize = 7, // different pool size
            //        ConnectionIdleTimeout = TimeSpan.FromMinutes(1)
            //    }));
            //}
            //catch (ArgumentException ae)
            //{
            //    Assert.IsTrue(ae.Message.Contains("AmqpTransportSettings cannot be modified from the initial settings."), "Did not return the correct error message");
            //}
        }

        #region Device Streaming

        private const string fakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        private const string fakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

        [TestMethod]
        public async Task AmqpTransportHandlerEnableStreamsAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().EnableStreamsAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerDisableStreamsAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().DisableStreamsAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerWaitForDeviceStreamRequestAsyncTokenCancellationRequested()
        {
            await TestOperationCanceledByToken(token => CreateFromConnectionString().WaitForDeviceStreamRequestAsync(token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerAcceptDeviceStreamRequestAsyncTokenCancellationRequested()
        {
            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);
            await TestOperationCanceledByToken(token => CreateFromConnectionString().AcceptDeviceStreamRequestAsync(request, token)).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AmqpTransportHandlerRejectDeviceStreamRequestAsyncTokenCancellationRequested()
        {
            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);
            await TestOperationCanceledByToken(token => CreateFromConnectionString().RejectDeviceStreamRequestAsync(request, token)).ConfigureAwait(false);
        }

        #endregion Device Streaming

        private async Task TestOperationCanceledByToken(Func<CancellationToken, Task> asyncMethod)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            try
            {
                await asyncMethod(tokenSource.Token).ConfigureAwait(false);
                Assert.Fail("Fail to skip execution of this operation using cancellation token.");
            }
            catch (OperationCanceledException) { }
        }

        private AmqpTransportHandler CreateFromConnectionString()
        {
            return new AmqpTransportHandler(
                new PipelineContext(),
                IotHubConnectionStringExtensions.Parse(DumpyConnectionString),
                new AmqpTransportSettings(TransportType.Amqp_Tcp_Only));
        }
    }
}
