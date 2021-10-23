// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport.Amqp;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    public class EdgeModuleClientTest
    {
        private readonly string _serverUrl;
        private readonly byte[] _sasKey = System.Text.Encoding.UTF8.GetBytes("key");
        private readonly string _iotHubConnectionString;

        private const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        private const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        private const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        private const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        private const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        private const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        private const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        private const string ModuleGeneratioIdVariableName = "IOTEDGE_MODULEGENERATIONID";

        public EdgeModuleClientTest()
        {
            _serverUrl = "http://localhost:8080";
            this._iotHubConnectionString = "Hostname=iothub.test;DeviceId=device1;ModuleId=module1;SharedAccessKey=" + Convert.ToBase64String(this._sasKey);
        }

        public async Task<ModuleClient> CreateAmqpModuleClientAsync()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this._serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            ModuleClient dc = await new EdgeModuleClientFactory(settings, trustBundle).CreateAsync();

            return dc;
        }
        
        public async Task<ModuleClient> CreateMqttModuleClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this._serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            ModuleClient dc = await new EdgeModuleClientFactory(settings, trustBundle).CreateAsync();

            return dc;
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var moduleClient = await CreateMqttModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var moduleClient = await CreateMqttModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(false, default).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);


            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Mqtt()
        {
            var moduleClient = await CreateMqttModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received().EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Mqtt()
        {
            var moduleClient = await CreateMqttModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Amqp()
        {
            var moduleClient = await CreateAmqpModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            var moduleClient = await CreateAmqpModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await moduleClient.SetInputMessageHandlerAsync("endpoint2", (message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint1", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetInputMessageHandlerAsync("endpoint2", null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_SetCallback_Amqp()
        {
            var moduleClient = await CreateAmqpModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ModuleClient_SetDefaultReceiveCallbackAsync_RemoveCallback_Amqp()
        {
            var moduleClient = await CreateAmqpModuleClient();
            IDelegatingHandler innerHandler = Substitute.For<IDelegatingHandler>();
            moduleClient.InnerHandler = innerHandler;

            await moduleClient.SetMessageHandlerAsync((message, context) => Task.FromResult(MessageResponse.Completed), "custom data").ConfigureAwait(false);

            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);

            await moduleClient.SetMessageHandlerAsync(null, null).ConfigureAwait(false);
            await innerHandler.Received(1).EnableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.DidNotReceiveWithAnyArgs().EnableReceiveMessageAsync(Arg.Any<CancellationToken>()).ConfigureAwait(false);
            await innerHandler.Received(1).DisableEventReceiveAsync(true, Arg.Any<CancellationToken>()).ConfigureAwait(false);
        }
    }
}
