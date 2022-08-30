// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Azure.Devices.Client.Test.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    [DoNotParallelize()]
    public class EdgeModuleClientHelperTest
    {
        private const string ServerUrl = "http://localhost:8080";

        private static readonly byte[] s_sasKey = Encoding.UTF8.GetBytes("key");
        private static readonly string s_iotHubConnectionString = "Hostname=iothub.test;DeviceId=device1;ModuleId=module1;SharedAccessKey=" + Convert.ToBase64String(s_sasKey);

        private const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        private const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        private const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        private const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        private const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        private const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        private const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";
        private const string ModuleGeneratioIdVariableName = "IOTEDGE_MODULEGENERATIONID";

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            using IotHubModuleClient dc = await IotHubModuleClient.CreateFromEnvironmentAsync();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_ShouldCreateClientWithOptions()
        {
            // setup
            var clientOptions = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                ModelId = "tempModuleId"
            };
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);

            // act
            using IotHubModuleClient dc = await IotHubModuleClient.CreateFromEnvironmentAsync(clientOptions);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportType_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using IotHubModuleClient dc = await IotHubModuleClient.CreateFromEnvironmentAsync(options);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            using IotHubModuleClient dc = await IotHubModuleClient.CreateFromEnvironmentAsync(options);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_MissingVariable_ShouldThrow()
        {
            try
            {
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
                Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
                Environment.SetEnvironmentVariable(ModuleIdVariableName, null);

                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());

                Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());
            }
            finally
            {
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
                Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
                Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            }
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_UnsupportedAuth_ShouldThrow()
        {
            try
            {
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
                Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
                Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");

                Environment.SetEnvironmentVariable(AuthSchemeVariableName, "x509Cert");
                TestAssert.Throws<InvalidOperationException>(() => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment());
            }
            finally
            {
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
                Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
                Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            }
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_SetAmqpTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientAmqpSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(trustBundle, options).ConfigureAwait(false);
            using IotHubModuleClient dc = new IotHubModuleClient(creds, options, certValidator);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromEnvironment_SetMqttTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientMqttSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(trustBundle, options).ConfigureAwait(false);
            using IotHubModuleClient dc = new IotHubModuleClient(creds, options, certValidator);

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }

        public async Task<IotHubModuleClient> CreateAmqpModuleClientAsync()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientAmqpSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(trustBundle, options).ConfigureAwait(false);

            // This client is being used by the test methods. It will be disposed by the respective tests.
            IotHubModuleClient dc = new IotHubModuleClient(creds, options, certValidator);

            return dc;
        }

        public async Task<IotHubModuleClient> CreateMqttModuleClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientMqttSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = Substitute.For<ITrustBundleProvider>();
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper.CreateCertificateValidatorFromEnvironmentAsync(trustBundle, options).ConfigureAwait(false);

            // // This client is being used by the test methods. It will be disposed by the respective tests.
            IotHubModuleClient dc = new IotHubModuleClient(creds, options, certValidator);

            return dc;
        }

        [TestMethod]
        public async Task ModuleClient_SetReceiveCallbackAsync_SetCallback_Mqtt()
        {
            using IotHubModuleClient moduleClient = await CreateMqttModuleClient();
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
            using IotHubModuleClient moduleClient = await CreateMqttModuleClient();
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
            using IotHubModuleClient moduleClient = await CreateMqttModuleClient();
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
            using IotHubModuleClient moduleClient = await CreateMqttModuleClient();
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
            using IotHubModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            using IotHubModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            using IotHubModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
            using IotHubModuleClient moduleClient = await CreateAmqpModuleClientAsync();
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
