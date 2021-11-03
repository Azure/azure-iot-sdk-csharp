// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    [DoNotParallelize()]
    public class EdgeModuleClientFactoryTest
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

        public EdgeModuleClientFactoryTest()
        {
            _serverUrl = "http://localhost:8080";
            this._iotHubConnectionString = "Hostname=iothub.test;DeviceId=device1;ModuleId=module1;SharedAccessKey=" + Convert.ToBase64String(this._sasKey);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this._iotHubConnectionString);
            ModuleClient dc = await ModuleClient.CreateFromEnvironmentAsync();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_ShouldCreateClientWithOptions()
        {
            // setup
            var clientOptions = new ClientOptions
            {
                ModelId = "tempModuleId"
            };
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this._iotHubConnectionString);

            // act
            ModuleClient dc = await ModuleClient.CreateFromEnvironmentAsync(clientOptions);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportType_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this._iotHubConnectionString);
            ModuleClient dc = await ModuleClient.CreateFromEnvironmentAsync(TransportType.Mqtt_Tcp_Only);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this._iotHubConnectionString);
            ModuleClient dc = await ModuleClient.CreateFromEnvironmentAsync(new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) });

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_MissingVariable_ShouldThrow()
        {
            var trustBundle = Substitute.For<ITrustBundleProvider>();

            var settings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this._serverUrl);
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            await TestAssert.ThrowsAsync<InvalidOperationException>(() => new EdgeModuleClientFactory(settings, trustBundle).CreateAsync()).ConfigureAwait(false);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_UnsupportedAuth_ShouldThrow()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this._serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");

            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "x509Cert");
            var settings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            await TestAssert
                .ThrowsAsync<InvalidOperationException>(async () => await new EdgeModuleClientFactory(settings, trustBundle).CreateAsync().ConfigureAwait(false))
                .ConfigureAwait(false);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_ShouldCreateClient()
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

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_SetTransportSettings_ShouldCreateClient()
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

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
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

            ITransportSettings[] settings = new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };
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

            ITransportSettings[] settings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            ModuleClient dc = await new EdgeModuleClientFactory(settings, trustBundle).CreateAsync();

            return dc;
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            ModuleClient moduleClient = await CreateMqttModuleClient();
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
            ModuleClient moduleClient = await CreateMqttModuleClient();
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
            ModuleClient moduleClient = await CreateMqttModuleClient();
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
            ModuleClient moduleClient = await CreateMqttModuleClient();
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
            ModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            ModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            ModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            ModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
