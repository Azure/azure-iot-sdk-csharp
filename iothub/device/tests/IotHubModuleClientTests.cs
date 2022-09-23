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
    [TestCategory("Unit")]
    public class IotHubModuleClientTests
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
            using var mc = new IotHubModuleClient(null);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionString_WithModuleId()
        {
            using var moduleClient = new IotHubModuleClient(ConnectionStringWithModuleId);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ModuleClient_CreateFromConnectionString_WithNoModuleIdThrows()
        {
            using var mc = new IotHubModuleClient(ConnectionStringWithoutModuleId);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionString_NoTransportSettings()
        {
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            Assert.IsNotNull(moduleClient);
        }

        [TestMethod]
        public void ModuleClient_CreateFromConnectionStringWithClientOptions_DoesNotThrow()
        {
            // setup
            var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings())
            {
                ModelId = "tempModuleId"
            };

            // act
            using var moduleClient = new IotHubModuleClient(FakeConnectionString, clientOptions);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using var moduleClient = new IotHubModuleClient(FakeConnectionString, options);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

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
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Amqp()
        {
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageAcknowledgementType.Complete), "custom data").ConfigureAwait(false);

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
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isMessageHandlerCalled = false;
            await moduleClient
                .SetMessageHandlerAsync(
                    (message, context) =>
                    {
                        isMessageHandlerCalled = true;
                        return Task.FromResult(MessageAcknowledgementType.Complete);
                    },
                    "custom data")
                .ConfigureAwait(false);

            await moduleClient.OnModuleEventMessageReceivedAsync(null).ConfigureAwait(false);
            Assert.IsFalse(isMessageHandlerCalled);
        }

        [TestMethod]
        public async Task ModuleClient_OnReceiveEventMessageCalled_DefaultCallbackCalled()
        {
            using var moduleClient = new IotHubModuleClient(FakeConnectionString);
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            bool isDefaultCallbackCalled = false;
            await moduleClient
                .SetMessageHandlerAsync(
                    (message, context) =>
                    {
                        isDefaultCallbackCalled = true;
                        return Task.FromResult(MessageAcknowledgementType.Complete);
                    },
                    "custom data")
                .ConfigureAwait(false);

            var testMessage = new Message
            {
                InputName = "endpoint1",
                LockToken = "AnyLockToken",
            };

            await moduleClient.OnModuleEventMessageReceivedAsync(testMessage).ConfigureAwait(false);
            Assert.IsTrue(isDefaultCallbackCalled);
        }
    }
}
