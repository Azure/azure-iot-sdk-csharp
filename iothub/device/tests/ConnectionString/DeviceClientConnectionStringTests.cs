// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    using System;
    using FluentAssertions;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.ApiTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringTests
    {
        const string LocalCertFilename = "..\\..\\Microsoft.Azure.Devices.Client.Test\\LocalNoChain.pfx";
        const string LocalCertPasswordFile = "..\\..\\Microsoft.Azure.Devices.Client.Test\\TestCertsPassword.txt";

        // TODO #583
        /* rewrite connection strin gparse tests 
        [TestMethod]
        public void DeviceClient_Create_DeviceScope_SharedAccessSignature_Test()
        {
            string hostName = "acme.azure-devices.net";
            string password = "dGVzdFN0cmluZzE=";
            var sasRule = new SharedAccessSignatureBuilder()
            {
                Key = password,
                Target = hostName + "/devices/" + "device1"
            };
            var authMethod = new DeviceAuthenticationWithToken("device1", sasRule.ToSignature());
            var deviceClient = AmqpTransportHandler.Create(hostName, authMethod);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }

        [TestMethod]
        public void DeviceClient_ConnectionString_DefaultScope_DefaultCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
        }

        [TestMethod]
        [Owner("HillaryC")]
        public void DeviceClient_ConnectionString_IotHubScope_ImplicitSharedAccessSignatureCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }

        [TestMethod]
        public void DeviceClient_ConnectionString_IotHubScope_ExplicitSharedAccessSignatureCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }

        [TestMethod]
        public void DeviceClient_ConnectionString_IotHubScope_SharedAccessKeyCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }
        */

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateDefaultTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod);
        }

        [TestMethod]
        public void DeviceClientConnectionStringX509CertTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=true;DeviceId=device";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringInvalidCharacterTest()
        {
            // The device Id has a semicolon, which is a character that is not allowed
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device;1";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateAmqpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp);
        }

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateAmqpWsTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateAmqpTcpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateHttpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Http1);
        }

        [TestMethod]
        [Ignore] // TODO #583
        public void DeviceClientConnectionStringX509CertificateMqttTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Mqtt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringX509CertificateNullCertificateTest()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_NoGatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, authMethod);
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_TransportType_NoGatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_TransportSettings_NoGatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
            });
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_GatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, gatewayHostname, authMethod);
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_TransportType_GatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, gatewayHostname, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        public void DeviceClient_AuthMethod_TransportSettings_GatewayHostnameTest()
        {
            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(hostName, gatewayHostname, authMethod, new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
            });
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/en-us/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_AuthMethod_TransparentGatewayHostnameTest()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(gatewayHostname, authMethod);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/en-us/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_AuthMethod_TransportType_TransparentGatewayHostnameTest()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(gatewayHostname, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        // This is for the scenario where an IoT Edge device is defined as the downstream device's transparent gateway.
        // For more details, see https://docs.microsoft.com/en-us/azure/iot-edge/how-to-authenticate-downstream-device#retrieve-and-modify-connection-string
        [TestMethod]
        public void DeviceClient_AuthMethod_TransportSettings_TransparentGatewayHostnameTest()
        {
            string gatewayHostname = "myGatewayDevice";
            var authMethod = new DeviceAuthenticationWithSakRefresh("device1", null);

            var deviceClient = DeviceClient.Create(gatewayHostname, authMethod, new ITransportSettings[]
            {
                new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
            });
        }

        [TestMethod]
        public void DeviceClientIotHubConnectionStringBuilderTest()
        {
            const string hostName = "acme.azure-devices.net";
            const string gatewayHostName = "gateway.acme.azure-devices.net";
            const string transparentGatewayHostName = "test";
            const string deviceId = "device1";
            const string deviceIdSplChar = "device1-.+%_#*?!(),=@$'";
            const string sharedAccessKey = "dGVzdFN0cmluZzE=";
            const string sharedAccessKeyName = "AllAccessKey";
            const string credentialScope = "Device";
            const string credentialType = "SharedAccessSignature";
            const string sharedAccessSignature = "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";

            string connectionString = $"HostName={hostName};SharedAccessKeyName={sharedAccessKeyName};DeviceId={deviceIdSplChar};SharedAccessKey={sharedAccessKey}";
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceIdSplChar);
            iotHubConnectionStringBuilder.GatewayHostName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessKeyName.Should().Be(sharedAccessKeyName);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();

            connectionString = $"HostName={hostName};GatewayHostName={transparentGatewayHostName};SharedAccessKeyName={sharedAccessKeyName};DeviceId={deviceId};SharedAccessKey={sharedAccessKey}";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.GatewayHostName.Should().Be(transparentGatewayHostName);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessKeyName.Should().Be(sharedAccessKeyName);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();

            connectionString = $"HostName={hostName};CredentialType={credentialType};DeviceId={deviceId};SharedAccessSignature={sharedAccessSignature}";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.GatewayHostName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            connectionString = $"HostName={hostName};CredentialScope={credentialScope};DeviceId={deviceId};SharedAccessKey={sharedAccessKey}";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.GatewayHostName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessKeyName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            connectionString = $"HostName={hostName};CredentialScope={credentialScope};DeviceId={deviceId};SharedAccessSignature={sharedAccessSignature}";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.GatewayHostName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKeyName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            iotHubConnectionStringBuilder.HostName = transparentGatewayHostName;
            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdSplChar, sharedAccessKey);
            iotHubConnectionStringBuilder.HostName.Should().Be(transparentGatewayHostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceIdSplChar);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            iotHubConnectionStringBuilder.HostName = hostName;
            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdSplChar, sharedAccessKey);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceIdSplChar);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithToken(deviceId, sharedAccessSignature);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, sharedAccessKeyName, sharedAccessKey);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithToken(deviceId, sharedAccessSignature);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            IAuthenticationMethod authMethod = AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sharedAccessSignature);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            authMethod = new DeviceAuthenticationWithSharedAccessPolicyKey(deviceId, sharedAccessKeyName, sharedAccessKey);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithSharedAccessPolicyKey>();

            authMethod = new DeviceAuthenticationWithToken(deviceId, sharedAccessSignature);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceId);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().Be(sharedAccessSignature);
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithToken>();

            authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdSplChar, sharedAccessKey);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.DeviceId.Should().Be(deviceIdSplChar);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            IAuthenticationMethod authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdSplChar, sharedAccessKey);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, gatewayHostName, authenticationMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.GatewayHostName.Should().Be(gatewayHostName);
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();

            authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey(deviceIdSplChar, sharedAccessKey);
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authenticationMethod);
            iotHubConnectionStringBuilder.HostName.Should().Be(hostName);
            iotHubConnectionStringBuilder.GatewayHostName.Should().BeNull();
            iotHubConnectionStringBuilder.SharedAccessKey.Should().Be(sharedAccessKey);
            iotHubConnectionStringBuilder.SharedAccessSignature.Should().BeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionStringBuilder.AuthenticationMethod.Should().BeOfType<DeviceAuthenticationWithRegistrySymmetricKey>();
        }
    }
}

