// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
            // arrange and act
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            await using var moduleClient = await IotHubModuleClient.CreateFromEnvironmentAsync();

            // assert
            moduleClient.Should().NotBeNull();

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_ShouldCreateClientWithOptions()
        {
            // arrange
            var clientOptions = new IotHubClientOptions(new IotHubClientAmqpSettings())
            {
                ModelId = "tempModuleId"
            };
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);

            // act
            await using var moduleClient = await IotHubModuleClient.CreateFromEnvironmentAsync(clientOptions);

            // assert
            moduleClient.Should().NotBeNull();

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportType_ShouldCreateClient()
        {
            // arrange and act
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            await using var moduleClient = await IotHubModuleClient.CreateFromEnvironmentAsync(options);

            // assert
            moduleClient.Should().NotBeNull();

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public async Task TestCreate_FromConnectionStringEnvironment_SetTransportSettings_ShouldCreateClient()
        {
            // arrange and act

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, s_iotHubConnectionString);
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
            await using var moduleClient = await IotHubModuleClient.CreateFromEnvironmentAsync(options);

            // assert
            moduleClient.Should().NotBeNull();

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_MissingVariable_ShouldThrow()
        {
            try
            {
                // arrange, act and assert
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
                Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
                Environment.SetEnvironmentVariable(ModuleIdVariableName, null);

                Action act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();

                Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
                act = () => _ = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                act.Should().Throw<InvalidOperationException>();
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
                // arrange
                Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
                Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
                Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
                Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
                Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
                Environment.SetEnvironmentVariable(AuthSchemeVariableName, "x509Cert");

                // act
                Action act = () => EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
                
                // assert
                act.Should().Throw<InvalidOperationException>();
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
            // arrange
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientAmqpSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = new Mock<ITrustBundleProvider>();
            trustBundle
                .Setup(x => x.GetTrustBundleAsync(It.IsAny<Uri>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult<IList<X509Certificate2>>(new List<X509Certificate2>(0)));
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper
                .CreateCertificateValidatorFromEnvironmentAsync(trustBundle.Object, options)
                .ConfigureAwait(false);

            // act
            await using var moduleClient = new IotHubModuleClient(creds, options, certValidator);

            // assert
            moduleClient.Should().NotBeNull();

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
            // arrange
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, ServerUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(ModuleGeneratioIdVariableName, "1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            var settings = new IotHubClientMqttSettings();
            var options = new IotHubClientOptions(settings);
            var trustBundle = new Mock<ITrustBundleProvider>();
            trustBundle
                .Setup(x => x.GetTrustBundleAsync(It.IsAny<Uri>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult<IList<X509Certificate2>>(new List<X509Certificate2>(0)));
            IotHubConnectionCredentials creds = EdgeModuleClientHelper.CreateIotHubConnectionCredentialsFromEnvironment();
            ICertificateValidator certValidator = await EdgeModuleClientHelper
                .CreateCertificateValidatorFromEnvironmentAsync(trustBundle.Object, options)
                .ConfigureAwait(false);

            // act
            await using var moduleClient = new IotHubModuleClient(creds, options, certValidator);

            // assert
            moduleClient.Should().NotBeNull();

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }
    }
}
