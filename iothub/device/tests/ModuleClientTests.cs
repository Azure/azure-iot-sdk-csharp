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
    public class ModuleClientTests
    {
        const string DeviceId = "module-twin-test";
        const string ModuleId = "mongo-server";
        const string ConnectionStringWithModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;ModuleId=mongo-server;SharedAccessKey=5zz6y/+wz4JtAloKTaBmAfmwfA2FhkK6mpADU7qDHcQ=";
        const string ConnectionStringWithoutModuleId = "GatewayHostName=edge.iot.microsoft.com;HostName=acme.azure-devices.net;DeviceId=module-twin-test;SharedAccessKey=5zz6y/+wz4JtAloKTaBmAfmwfA2FhkK6mpADU7qDHcQ=";

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
        [TestCategory("DeviceClient")]
        [ExpectedException(typeof(ArgumentNullException))]
        // Tests_SRS_DEVICECLIENT_10_001: [** if `connectionString` is null, an `ArgumentNullException` shall be thrown. **]**
        public void ModuleClient_CreateFromConnectionString_NullConnectionString()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(null);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public void ModuleClient_CreateFromConnectionString_NoTransportSettings()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
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
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_003: [** It shall EnableEventReceiveAsync when called for the first time. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data");

            await innerHandler.Received().EnableEventReceiveAsync(Arg.Any<CancellationToken>());
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(Arg.Any<CancellationToken>());
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_004: [** It shall call DisableEventReceiveAsync when the last delegate has been removed. **]**
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
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
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_001: [** If the given eventMessageInternal argument is null, fail silently **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_NullMessageRequest()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
            var innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isMessageHandlerCalled = false;
            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) =>
            {
                isMessageHandlerCalled = true;
                return Task.FromResult(MessageResponse.Completed);
            }, "custom data");

            await moduleClient.OnReceiveEventMessageCalled(null, null);
            Assert.IsFalse(isMessageHandlerCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_006: [** The OnReceiveEventMessageCalled shall get the default delegate if a delegate has not been assigned. **]**
        // Tests_SRS_DEVICECLIENT_33_005: [** It shall lazy-initialize the receiveEventEndpoints property. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
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

            await moduleClient.OnReceiveEventMessageCalled("endpoint1", testMessage);
            Assert.IsTrue(isDefaultCallbackCalled);
            Assert.IsFalse(isSpecificCallbackCalled);
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        // Tests_SRS_DEVICECLIENT_33_002: [** The OnReceiveEventMessageCalled shall invoke the specified delegate. **]**
        public async Task ModuleClient_OnReceiveEventMessageCalled_SpecifiedCallbackCalled()
        {
            DeviceClient moduleClient = DeviceClient.CreateFromConnectionString(fakeConnectionString, TransportType.Mqtt_Tcp_Only);
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

            await moduleClient.OnReceiveEventMessageCalled("endpoint2", testMessage);
            Assert.IsFalse(isDefaultCallbackCalled);
            Assert.IsTrue(isSpecificCallbackCalled);
        }        
    }
}
