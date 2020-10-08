// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("DeviceStreaming")]
    [TestCategory("Unit")]
    public class DeviceStreamingTests
    {
        private const string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";

        const string fakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        const string fakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

        #region Device Streaming
        [TestMethod]
        public async Task DeviceClientWaitForDeviceStreamRequestAsync()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();
            
            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync(ct);

            await innerHandler.Received().EnableStreamsAsync(ct).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientWaitForDeviceStreamRequestAsyncNoCancellationToken()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync();

            await innerHandler.Received().EnableStreamsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientAcceptDeviceStreamRequestAsync()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.AcceptDeviceStreamRequestAsync(request, ct);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientAcceptDeviceStreamRequestAsyncNoCancellationToken()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.AcceptDeviceStreamRequestAsync(request);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientRejectDeviceStreamRequestAsync()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.RejectDeviceStreamRequestAsync(request, ct);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientRejectDeviceStreamRequestAsyncNoCancellationToken()
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.RejectDeviceStreamRequestAsync(request);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }
        #endregion Device Streaming
    }
}
