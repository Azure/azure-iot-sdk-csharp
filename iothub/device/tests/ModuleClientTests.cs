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
            using var moduleClient = ModuleClient.CreateFromConnectionString(ConnectionStringWithModuleId);
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
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
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
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new ClientOptions(new MqttTransportSettings());
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var options = new ClientOptions(new MqttTransportSettings());
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, default).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);


            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new ClientOptions(new MqttTransportSettings());
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var options = new ClientOptions(new MqttTransportSettings());
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Amqp()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Amqp()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_NullMessageRequest()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
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

            await moduleClient.InternalClient.OnModuleEventMessageReceivedAsync(null, null).ConfigureAwait(false);
            Assert.IsFalse(isMessageHandlerCalled);
        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
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

            await moduleClient.InternalClient.OnModuleEventMessageReceivedAsync("endpoint1", testMessage).ConfigureAwait(false);
            Assert.IsTrue(isDefaultCallbackCalled);
            Assert.IsFalse(isSpecificCallbackCalled);
        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_SpecifiedCallbackCalled()
        {
            var moduleClient = ModuleClient.CreateFromConnectionString(FakeConnectionString);
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

            await moduleClient.InternalClient.OnModuleEventMessageReceivedAsync("endpoint2", testMessage).ConfigureAwait(false);
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
    }
}
