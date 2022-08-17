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
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.HostName.Should().Be(HostName);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesDeviceId()
        {
            var connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.DeviceId.Should().Be(DeviceId);
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
                    var csBuilder = new IotHubConnectionCredentials(connectionString);
                    csBuilder.DeviceId.Should().Be(deviceId, $"failed to match in {culture}");
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
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.ModuleId.Should().Be(ModuleId);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesComplexDeviceId()
        {
            var connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};DeviceId={DeviceIdSplChar};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.DeviceId.Should().Be(DeviceIdSplChar);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessKey()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);

            csBuilder.SharedAccessKey.Should().Be(SharedAccessKey);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            csBuilder.SharedAccessSignature.Should().BeNull("SharedAccessKey and SharedAccessSignature are mutually exclusive");
            csBuilder.UsingX509Cert.Should().BeFalse("SharedAccessKey and X509 are mutually exclusive");
        }

        [TestMethod]
        [DataRow("X509Cert")]
        [DataRow("X509")]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesX509False(string x509)
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey={SharedAccessKey};{x509}=false";
            var csBuilder = new IotHubConnectionCredentials(connectionString);

            csBuilder.SharedAccessKey.Should().Be(SharedAccessKey);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();
            csBuilder.UsingX509Cert.Should().BeFalse("SharedAccessKey and X509 are mutually exclusive");
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessKeyName()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.SharedAccessKeyName.Should().Be(SharedAccessKeyName);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesSharedAccessSignature()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessSignature={SharedAccessSignature}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);

            csBuilder.SharedAccessSignature.Should().Be(SharedAccessSignature);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            csBuilder.SharedAccessKey.Should().BeNull("SharedAccessSignature and SharedAccessKey are mutually exclusive");
            csBuilder.UsingX509Cert.Should().BeFalse("SharedAccessSignature and X509 are mutually exclusive");
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_OverrideAuthMethodToken()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessSignature={SharedAccessSignature}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.AuthenticationMethod = new DeviceAuthenticationWithToken(DeviceId, SharedAccessSignature);

            csBuilder.SharedAccessSignature.Should().Be(SharedAccessSignature);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_OverrideAuthMethodSapk()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessSignature={SharedAccessSignature}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.AuthenticationMethod = new DeviceAuthenticationWithSharedAccessPolicyKey(DeviceId, SharedAccessKeyName, SharedAccessKey);

            csBuilder.SharedAccessKey.Should().Be(SharedAccessKey);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();
        }

        [TestMethod]
        [DataRow("true")]
        [DataRow("True")]
        [DataRow("TRUE")]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesX509BoolCaseInsensitive(string value)
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};X509Cert={value}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);

            csBuilder.UsingX509Cert.Should().BeTrue();

            csBuilder.SharedAccessKey.Should().BeNull();
            csBuilder.SharedAccessKeyName.Should().BeNull();
            csBuilder.SharedAccessSignature.Should().BeNull();
        }

        /// <summary>
        /// Ensure we support both and either x509Cert= and x509= in our connection string builder/parser, for backward compat and alignment with other SDKs.
        /// If either is true, then we'll consider it true.
        /// </summary>
        [TestMethod]
        [DataRow("true", "true")]
        [DataRow("false", "true")]
        [DataRow(null, "true")]
        [DataRow("true", "false")]
        [DataRow("true", null)]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesX509Mix(string x509CertValue, string x509Value)
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId}";
            if (x509CertValue != null)
            {
                connectionString += $";X509Cert={x509CertValue}";
            }
            if (x509Value != null)
            {
                connectionString += $";x509={x509Value}";
            }
            var csBuilder = new IotHubConnectionCredentials(connectionString);

            csBuilder.UsingX509Cert.Should().BeTrue();

            csBuilder.SharedAccessKey.Should().BeNull();
            csBuilder.SharedAccessKeyName.Should().BeNull();
            csBuilder.SharedAccessSignature.Should().BeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamConnectionString_ParsesGatewayHostName()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId};GatewayHostName={TransparentGatewayHostName};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
            csBuilder.GatewayHostName.Should().Be(TransparentGatewayHostName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_MissingHostName_Throws()
        {
            var connectionString = $"DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_MissingDeviceId_Throws()
        {
            var connectionString = $"HostName={HostName};SharedAccessKey={SharedAccessKey}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubConnectionStringBuilder_ParamConnectionString_NoAuthSpecied_Throws()
        {
            var connectionString = $"HostName={HostName};DeviceId={DeviceId}";
            var csBuilder = new IotHubConnectionCredentials(connectionString);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_SharedAccessKey()
        {
            var authMethod = new DeviceAuthenticationWithSharedAccessPolicyKey(DeviceId, SharedAccessKeyName, SharedAccessKey);
            var csBuilder = new IotHubConnectionCredentials(authMethod, HostName);
            csBuilder.HostName.Should().Be(HostName);
            csBuilder.DeviceId.Should().Be(DeviceId);
            csBuilder.SharedAccessKey.Should().Be(SharedAccessKey);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();

            csBuilder.SharedAccessSignature.Should().BeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_SharedAccessSignature()
        {
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithToken(DeviceId, SharedAccessSignature);
            var csBuilder = new IotHubConnectionCredentials(authMethod, HostName);
            csBuilder.HostName.Should().Be(HostName);
            csBuilder.DeviceId.Should().Be(DeviceId);
            csBuilder.SharedAccessSignature.Should().Be(SharedAccessSignature);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            csBuilder.SharedAccessKey.Should().BeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameAuthMethod_DeviceIdComplex()
        {
            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(DeviceIdSplChar, SharedAccessKey);
            var csBuilder = new IotHubConnectionCredentials(authMethod, HostName);
            csBuilder.DeviceId.Should().Be(DeviceIdSplChar);
        }

        [TestMethod]
        public void IotHubConnectionStringBuilder_ParamHostNameGatewayAuthMethod_Basic()
        {
            IAuthenticationMethod authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, SharedAccessKey);
            var csBuilder = new IotHubConnectionCredentials(authMethod, HostName, GatewayHostName);
            csBuilder.HostName.Should().Be(HostName);
            csBuilder.DeviceId.Should().Be(DeviceId);
            csBuilder.GatewayHostName.Should().Be(GatewayHostName);
            csBuilder.SharedAccessKey.Should().Be(SharedAccessKey);
            csBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            csBuilder.SharedAccessSignature.Should().BeNull();
        }
    }
}
