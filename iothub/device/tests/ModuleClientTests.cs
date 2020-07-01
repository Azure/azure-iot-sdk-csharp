﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ModuleClientTests
    {
        private const string DeviceId = "module-twin-test";
        private const string ModuleId = "mongo-server";
        private const string ConnectionStringWithModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;ModuleId=mongo-server;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string ConnectionStringWithoutModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;SharedAccessKey=dGVzdFN0cmluZzQ=";
        private const string FakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=dGVzdFN0cmluZzE=";

        public const string NoModuleTwinJson = "{ \"maxConnections\": 10 }";

        private const string fakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        private const string fakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

        public readonly string ValidDeviceTwinJson = string.Format(
            @"
{{
    ""{1}"": {{
        ""properties"": {{
            ""desired"": {{
                ""maxConnections"": 10,
                ""$metadata"": {{
                    ""$lastUpdated"": ""2017-05-30T22:37:31.1441889Z"",
                    ""$lastUpdatedVersion"": 2
                }}
            }}
        }}
    }},
    ""nginx-server"": {{
        ""properties"": {{
            ""desired"": {{
                ""forwardUrl"": ""http://example.com""
            }}
        }}
    }}
}}
",
            DeviceId,
            ModuleId);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ModuleClient_CreateFromConnectionString_NullConnectionStringThrows()
        {
            _ = ModuleClient.CreateFromConnectionString(null);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionString_WithModuleId()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(ConnectionStringWithModuleId);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ModuleClient_CreateFromConnectionString_WithNoModuleIdThrows()
        {
            ModuleClient.CreateFromConnectionString(ConnectionStringWithoutModuleId);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionString_NoTransportSettings()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionStringWithClientOptions_DoesNotThrow()
        {
            // setup
            var clientOptions = new ClientOptions
            {
                ModelId = "tempModuleId"
            };

            // act
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, clientOptions);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(default).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_001: [** If the given eventMessageInternal argument is null, fail silently **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_NullMessageRequest()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isMessageHandlerCalled = false;
            await moduleClient
                .SetInputMessageHandlerAsync(
                    "endpoint1",
                    (message, context) =>
                    {
                        isMessageHandlerCalled = true;
                        return Task.FromResult(MessageResponse.Completed);
                    },
                    "custom data")
                .ConfigureAwait(false);

            await moduleClient.InternalClient.OnReceiveEventMessageCalled(null, null).ConfigureAwait(false);
            Assert.IsFalse(isMessageHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_006: [** The OnReceiveEventMessageCalled shall get the default delegate if a delegate has not been assigned. **]**
        // Tests_SRS_DEVICECLIENT_33_005: [** It shall lazy-initialize the receiveEventEndpoints property. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isDefaultCallbackCalled = false;
            await moduleClient
                .SetMessageHandlerAsync(
                    (message, context) =>
                    {
                        isDefaultCallbackCalled = true;
                        return Task.FromResult(MessageResponse.Completed);
                    },
                    "custom data")
                .ConfigureAwait(false);

            bool isSpecificCallbackCalled = false;
            await moduleClient
                .SetInputMessageHandlerAsync(
                    "endpoint2", (message, context) =>

                     {
                         isSpecificCallbackCalled = true;
                         return Task.FromResult(MessageResponse.Completed);
                     },
                    "custom data")
                .ConfigureAwait(false);

            var testMessage = new Message
            {
                LockToken = "AnyLockToken",
            };

            await moduleClient.InternalClient.OnReceiveEventMessageCalled("endpoint1", testMessage).ConfigureAwait(false);
            Assert.IsTrue(isDefaultCallbackCalled);
            Assert.IsFalse(isSpecificCallbackCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_002: [** The OnReceiveEventMessageCalled shall invoke the specified delegate. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_SpecifiedCallbackCalled()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, TransportType.Mqtt_Tcp_Only);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isDefaultCallbackCalled = false;
            await moduleClient.SetMessageHandlerAsync(
                (message, context) =>
                {
                    isDefaultCallbackCalled = true;
                    return Task.FromResult(MessageResponse.Completed);
                },
                "custom data");

            bool isSpecificCallbackCalled = false;
            await moduleClient.SetInputMessageHandlerAsync(
                "endpoint2",
                (message, context) =>
                {
                    isSpecificCallbackCalled = true;
                    return Task.FromResult(MessageResponse.Completed);
                },
                "custom data");

            var testMessage = new Message
            {
                LockToken = "AnyLockToken",
            };

            await moduleClient.InternalClient.OnReceiveEventMessageCalled("endpoint2", testMessage).ConfigureAwait(false);
            Assert.IsFalse(isDefaultCallbackCalled);
            Assert.IsTrue(isSpecificCallbackCalled);
        }

        [TestMethod]
        public void ModuleClient_InvokeMethodAsyncWithoutBodyShouldNotThrow()
        {
            // arrange
            var request = new MethodRequest("test");

            // act
            _ = new MethodInvokeRequest(request.Name, request.DataAsJson, request.ResponseTimeout, request.ConnectionTimeout);
        }

        #region Module Streaming

        [TestMethod]
        public async Task ModuleClientWaitForDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();

            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync(ct);

            await innerHandler.Received().EnableStreamsAsync(ct).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientWaitForDeviceStreamRequestAsyncNoCancellationToken()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync();

            await innerHandler.Received().EnableStreamsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientAcceptDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.AcceptDeviceStreamRequestAsync(request, ct);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientAcceptDeviceStreamRequestAsyncNoCancellationToken()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.AcceptDeviceStreamRequestAsync(request);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientRejectDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            CancellationToken ct = new CancellationToken();

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.RejectDeviceStreamRequestAsync(request, ct);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientRejectDeviceStreamRequestAsyncNoCancellationToken()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.RejectDeviceStreamRequestAsync(request);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        #endregion Module Streaming
    }
}
