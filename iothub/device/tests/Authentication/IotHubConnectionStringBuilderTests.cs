// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.ApiTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubConnectionStringBuilderTests
    {
        private const string HostName = "acme.azure-devices.net";
        private const string GatewayHostName = "gateway.acme.azure-devices.net";
        private const string TransparentGatewayHostName = "test";
        private const string DeviceId = "device1";
        private const string DeviceIdSplChar = "device1-.+%_#*?!(),=@;$'";
        private const string ModuleId = "moduleId";
        private const string SharedAccessKey = "dGVzdFN0cmluZzE=";
        private const string SharedAccessKeyName = "AllAccessKey";
        private const string CredentialScope = "Device";
        private const string CredentialType = "SharedAccessSignature";
        private const string SharedAccessSignature = "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesHostName()
        {
            var connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.IotHubHostName.Should().Be(HostName);
            iotHubConnectionCredentials.HostName.Should().Be(HostName);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesDeviceId()
        {
            var connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceId);
        }

        [TestMethod]
        [DoNotParallelize]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesCultureDeviceId()
        {
            const string deviceId = "IoTDeviceCheck";

            CultureInfo savedCultureInfo = Thread.CurrentThread.CurrentCulture;
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            try
            {
                foreach (CultureInfo culture in allCultures)
                {
                    Console.WriteLine($"Testing culture {culture}");
                    Thread.CurrentThread.CurrentCulture = culture;
                    var connectionString = $"HostName={HostName};DeviceId={deviceId};SharedAccessKey={SharedAccessKey}";
                    var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
                    iotHubConnectionCredentials.DeviceId.Should().Be(deviceId, $"failed to match in {culture}");
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = savedCultureInfo;
            }
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesModuleId()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};ModuleId={ModuleId};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.ModuleId.Should().Be(ModuleId);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesComplexDeviceId()
        {
            var connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};DeviceId={DeviceIdSplChar};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceIdSplChar);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessKey()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);

            iotHubConnectionCredentials.SharedAccessKey.Should().Be(SharedAccessKey);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithRegistrySymmetricKey>();

            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull("SharedAccessKey and SharedAccessSignature are mutually exclusive");
            iotHubConnectionCredentials.Certificate.Should().BeNull("SharedAccessKey and X.509 are mutually exclusive");
        }

        [TestMethod]
        [DataRow("X509Cert")]
        [DataRow("X509")]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesX509False(string x509)
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey};{x509}=false";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);

            iotHubConnectionCredentials.SharedAccessKey.Should().Be(SharedAccessKey);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithRegistrySymmetricKey>();
            iotHubConnectionCredentials.Certificate.Should().BeNull("SharedAccessKey and X.509 are mutually exclusive");
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessKeyName()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.SharedAccessKeyName.Should().Be(SharedAccessKeyName);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessPolicy>();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessSignature()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessSignature={SharedAccessSignature}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);

            iotHubConnectionCredentials.SharedAccessSignature.Should().Be(SharedAccessSignature);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessSignature>();

            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull("SharedAccessSignature and SharedAccessKey are mutually exclusive");
            iotHubConnectionCredentials.Certificate.Should().BeNull("SharedAccessSignature and X.509 are mutually exclusive");
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesGatewayHostName()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};GatewayHostName={TransparentGatewayHostName};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
            iotHubConnectionCredentials.IotHubHostName.Should().Be(HostName);
            iotHubConnectionCredentials.GatewayHostName.Should().Be(TransparentGatewayHostName);
            iotHubConnectionCredentials.HostName.Should().Be(TransparentGatewayHostName);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_MissingHostName_Throws()
        {
            var connectionString = $"DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_MissingDeviceId_Throws()
        {
            var connectionString = $"HostName={HostName};SharedAccessKey={SharedAccessKey}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_NoAuthSpecied_Throws()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId}";
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_SharedAccessKey()
        {
            var authMethod = new ClientAuthenticationWithSharedAccessPolicy(SharedAccessKeyName, SharedAccessKey, DeviceId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(authMethod, HostName);
            iotHubConnectionCredentials.IotHubHostName.Should().Be(HostName);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceId);
            iotHubConnectionCredentials.SharedAccessKey.Should().Be(SharedAccessKey);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessPolicy>();

            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_SharedAccessSignature()
        {
            IAuthenticationMethod authMethod = new ClientAuthenticationWithSharedAccessSignature(SharedAccessSignature, DeviceId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(authMethod, HostName);
            iotHubConnectionCredentials.IotHubHostName.Should().Be(HostName);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceId);
            iotHubConnectionCredentials.SharedAccessSignature.Should().Be(SharedAccessSignature);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessSignature>();

            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_DeviceIdComplex()
        {
            var authMethod = new ClientAuthenticationWithRegistrySymmetricKey(SharedAccessKey, DeviceIdSplChar);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(authMethod, HostName);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceIdSplChar);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameGatewayAuthMethod_Basic()
        {
            IAuthenticationMethod authMethod = new ClientAuthenticationWithRegistrySymmetricKey(SharedAccessKey, DeviceId);
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(authMethod, HostName, GatewayHostName);
            iotHubConnectionCredentials.IotHubHostName.Should().Be(HostName);
            iotHubConnectionCredentials.DeviceId.Should().Be(DeviceId);
            iotHubConnectionCredentials.GatewayHostName.Should().Be(GatewayHostName);
            iotHubConnectionCredentials.HostName.Should().Be(GatewayHostName);
            iotHubConnectionCredentials.SharedAccessKey.Should().Be(SharedAccessKey);
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithRegistrySymmetricKey>();

            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
        }
    }
}
