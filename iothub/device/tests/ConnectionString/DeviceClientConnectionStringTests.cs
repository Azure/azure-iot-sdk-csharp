// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.ApiTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringTests
    {
        const string LocalCertFilename = "..\\..\\Microsoft.Azure.Devices.Client.Test\\LocalNoChain.pfx";
        const string LocalCertPasswordFile = "..\\..\\Microsoft.Azure.Devices.Client.Test\\TestCertsPassword.txt";

        /* rewrite connection strin gparse tests
        [TestMethod]
        public void DeviceClient_Create_DeviceScope_SharedAccessSignature_Test()
        {
            string hostName = "acme.azure-devices.net";
            string password = "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
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
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
        }

        [TestMethod]
        [Owner("HillaryC")]
        public void DeviceClient_ConnectionString_IotHubScope_ImplicitSharedAccessSignatureCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }

        [TestMethod]
        public void DeviceClient_ConnectionString_IotHubScope_ExplicitSharedAccessSignatureCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }

        [TestMethod]
        public void DeviceClient_ConnectionString_IotHubScope_SharedAccessKeyCredentialType_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var deviceClient = AmqpTransportHandler.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(deviceClient.IotHubConnection);
            Assert.IsNotNull(((IotHubSingleTokenConnection)deviceClient.IotHubConnection).ConnectionString);
        }
        */

        [TestMethod]
        [Ignore]
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
        [Ignore]
        public void DeviceClientConnectionStringX509CertificateAmqpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp);
        }

        [TestMethod]
        [Ignore]
        public void DeviceClientConnectionStringX509CertificateAmqpWsTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_WebSocket_Only);
        }

        [TestMethod]
        [Ignore]
        public void DeviceClientConnectionStringX509CertificateAmqpTcpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Amqp_Tcp_Only);
        }

        [TestMethod]
        [Ignore]
        public void DeviceClientConnectionStringX509CertificateHttpTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, TransportType.Http1);
        }

        [TestMethod]
        [Ignore]
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

        [TestMethod]
        public void DeviceClientIotHubConnectionStringBuilderTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithSharedAccessPolicyKey);

            connectionString = "HostName=acme.azure-devices.net;GatewayHostName=test;SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNotNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithSharedAccessPolicyKey);

            connectionString = "HostName=acme.azure-devices.net;CredentialType=SharedAccessSignature;DeviceId=device1;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);

            connectionString = "HostName=acme.azure-devices.net;CredentialScope=Device;DeviceId=device1;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithRegistrySymmetricKey);

            connectionString = "HostName=acme.azure-devices.net;CredentialScope=Device;DeviceId=device1;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);

            try
            {
                iotHubConnectionStringBuilder.HostName = "adshgfvyregferuehfiuehr";
                Assert.Fail("Expected FormatException");
            }
            catch (FormatException)
            {               
            }

            iotHubConnectionStringBuilder.HostName = "acme.azure-devices.net";
            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey("Device1", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithRegistrySymmetricKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId=="Device1");

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithToken("Device2", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device2");

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithSharedAccessPolicyKey("Device3", "AllAccess", "KQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithSharedAccessPolicyKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "KQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device3");

            iotHubConnectionStringBuilder.AuthenticationMethod = new DeviceAuthenticationWithToken("Device4", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device4");

            IAuthenticationMethod authMethod = AuthenticationMethodFactory.CreateAuthenticationWithToken("Device5", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme1.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme1.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device5");

            authMethod = new DeviceAuthenticationWithSharedAccessPolicyKey("Device3", "AllAccess", "KQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme2.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithSharedAccessPolicyKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme2.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "KQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device3");

            authMethod = new DeviceAuthenticationWithToken("Device2", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme3.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme3.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=poifbMLdBGtCJknubF2FW6FLn5vND5k1IKoeQ%2bONgkE%3d&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device2");

            authMethod = new DeviceAuthenticationWithRegistrySymmetricKey("Device1", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme4.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithRegistrySymmetricKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme4.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            Assert.IsTrue(iotHubConnectionStringBuilder.DeviceId == "Device1");

            string hostName = "acme.azure-devices.net";
            string gatewayHostname = "gateway.acme.azure-devices.net";
            IAuthenticationMethod authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey("Device1", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, gatewayHostname, authenticationMethod);
            Assert.AreEqual(gatewayHostname, iotHubConnectionStringBuilder.GatewayHostName);
            Assert.AreEqual(hostName, iotHubConnectionStringBuilder.HostName);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithRegistrySymmetricKey);

            hostName = "acme.azure-devices.net";
            authenticationMethod = new DeviceAuthenticationWithRegistrySymmetricKey("Device1", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(hostName, authenticationMethod);
            Assert.AreEqual(hostName, iotHubConnectionStringBuilder.HostName);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is DeviceAuthenticationWithRegistrySymmetricKey);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
        }
    }
}

