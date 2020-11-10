// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;SharedAccessKey=dGVzdFN0cmluZzE=";
        private const string FakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        private const string FakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

        [TestMethod]
        public async Task DeviceClientWaitForDeviceStreamRequestAsync()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync();

            await innerHandler.Received().EnableStreamsAsync(default).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(default).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientAcceptDeviceStreamRequestAsync()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            var request = new DeviceStreamRequest("1", "StreamA", new Uri(FakeDeviceStreamSGWUrl), FakeDeviceStreamAuthToken);

            _ = deviceClient.AcceptDeviceStreamRequestAsync(request);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, default).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeviceClientRejectDeviceStreamRequestAsync()
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            var request = new DeviceStreamRequest("1", "StreamA", new Uri(FakeDeviceStreamSGWUrl), FakeDeviceStreamAuthToken);

            _ = deviceClient.RejectDeviceStreamRequestAsync(request);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, default).ConfigureAwait(false);
        }
    }
}
