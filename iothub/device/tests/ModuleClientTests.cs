namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Devices.Client.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using Moq;
    using Newtonsoft.Json.Linq;

    [TestClass]
    [TestCategory("Unit")]
    public class ModuleClientTests
    {
        const string DeviceId = "module-twin-test";
        const string ModuleId = "mongo-server";
        const string ConnectionStringWithModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;ModuleId=mongo-server;SharedAccessKey=5zz6y/+wz4JtAloKTaBmAfmwfA2FhkK6mpADU7qDHcQ=";
        const string ConnectionStringWithoutModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;SharedAccessKey=5zz6y/+wz4JtAloKTaBmAfmwfA2FhkK6mpADU7qDHcQ=";

        const string fakeDeviceStreamSGWUrl = "wss://sgw.eastus2euap-001.streams.azure-devices.net/bridges/iot-sdks-tcpstreaming/E2E_DeviceStreamingTests_Sasl_f88fd19b-ed0d-496b-b32c-6346ca61d289/E2E_DeviceStreamingTests_b82c9ec4-4fb3-432a-bfb5-af484966a7d4c002f7a841b8/3a6a2eba4b525c38bfcb";
        const string fakeDeviceStreamAuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJleHAiOjE1NDgzNTU0ODEsImp0aSI6InFfdlllQkF4OGpmRW5tTWFpOHhSNTM2QkpxdTZfRlBOa2ZWSFJieUc4bUUiLCJpb3RodWIRrcy10Y3BzdHJlYW1pbmciOiJpb3Qtc2ifQ.X_HIb53nDsCT2SZ0P4-vnA_Wz94jxYRLbk_5nvP9bj8";

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

        public readonly string NoModuleTwinJson = "{ \"maxConnections\": 10 }";

        static string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        // Tests_SRS_DEVICECLIENT_10_001: [** if `connectionString` is null, an `ArgumentNullException` shall be thrown. **]**
        public void ModuleClient_CreateFromConnectionString_NullConnectionString()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(null);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionString_WithModuleId()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(ConnectionStringWithModuleId);
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
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data");
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());

            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.Received(1).DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());

            await moduleClient.SetMessageHandlerAsync(null, null);
            await innerHandler.Received(1).EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.Received(1).DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }


        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_001: [** If the given eventMessageInternal argument is null, fail silently **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_NullMessageRequest()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isMessageHandlerCalled = false;
            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) =>
            {
                isMessageHandlerCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            await moduleClient.InternalClient.OnReceiveEventMessageCalled(null, null);
            Assert.IsFalse(isMessageHandlerCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_006: [** The OnReceiveEventMessageCalled shall get the default delegate if a delegate has not been assigned. **]**
        // Tests_SRS_DEVICECLIENT_33_005: [** It shall lazy-initialize the receiveEventEndpoints property. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isDefaultCallbackCalled = false;
            await moduleClient.SetMessageHandlerAsync((message, context) =>
            {
                isDefaultCallbackCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            bool isSpecificCallbackCalled = false;
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) =>
            {
                isSpecificCallbackCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            Message testMessage = new Message();
            testMessage.LockToken = "AnyLockToken";

            await moduleClient.InternalClient.OnReceiveEventMessageCalled("endpoint1", testMessage);
            Assert.IsTrue(isDefaultCallbackCalled);
            Assert.IsFalse(isSpecificCallbackCalled);
        }

        [TestMethod]
        // Tests_SRS_DEVICECLIENT_33_002: [** The OnReceiveEventMessageCalled shall invoke the specified delegate. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_SpecifiedCallbackCalled()
        {
            ModuleClient moduleClient = ModuleClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isDefaultCallbackCalled = false;
            await moduleClient.SetMessageHandlerAsync((message, context) =>
            {
                isDefaultCallbackCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            bool isSpecificCallbackCalled = false;
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) =>
            {
                isSpecificCallbackCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            Message testMessage = new Message();
            testMessage.LockToken = "AnyLockToken";

            await moduleClient.InternalClient.OnReceiveEventMessageCalled("endpoint2", testMessage);
            Assert.IsFalse(isDefaultCallbackCalled);
            Assert.IsTrue(isSpecificCallbackCalled);
        }

        [TestMethod]
        public async Task ModuleClient_InvokeMethodAsyncWithoutBody()
        {
            var request = new MethodRequest("test");
            var invokeRequest = new MethodInvokeRequest(request.Name, request.DataAsJson, request.ResponseTimeout, request.ConnectionTimeout);
            Assert.IsTrue(invokeRequest != null);
        }

        #region Module Streaming
        [TestMethod]
        public async Task ModuleClientWaitForDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

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
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            Task<DeviceStreamRequest> requestTask = deviceClient.WaitForDeviceStreamRequestAsync();

            await innerHandler.Received().EnableStreamsAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received().WaitForDeviceStreamRequestAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientAcceptDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

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
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.AcceptDeviceStreamRequestAsync(request);

            await innerHandler.Received().AcceptDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClientRejectDeviceStreamRequestAsync()
        {
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

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
            ModuleClient deviceClient = ModuleClient.CreateFromConnectionString(fakeConnectionString);

            var innerHandler = Substitute.For<IDelegatingHandler>();
            deviceClient.InnerHandler = innerHandler;

            DeviceStreamRequest request = new DeviceStreamRequest("1", "StreamA", new Uri(fakeDeviceStreamSGWUrl), fakeDeviceStreamAuthToken);

            Task acceptTask = deviceClient.RejectDeviceStreamRequestAsync(request);

            await innerHandler.Received().RejectDeviceStreamRequestAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }
        #endregion Module Streaming
    }
}
