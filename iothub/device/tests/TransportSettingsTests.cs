// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

namespace Microsoft.Azure.Devices.Client.Test
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.ApiTest;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class TransportSettingsTests
    {
        const string LocalCertFilename = "..\\..\\Microsoft.Azure.Devices.Client.Test\\LocalNoChain.pfx";
        const string LocalCertPasswordFile = "..\\..\\Microsoft.Azure.Devices.Client.Test\\TestCertsPassword.txt";

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void TransportSettingsTestTransportTypeAmqp()
        {
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void TransportSettingsTestTransportTypeAmqpHttp()
        {
            var transportSetting = new AmqpTransportSettings(TransportType.Http1);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void TransportSettingsTestTransportTypeAmqpTcpPrefetch0()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 0, amqpConnectionPoolSettings);
        }

        [TestMethod]
        public void TransportSettingsTestTransportTypeAmqpWebSocket()
        {
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only);
            Assert.IsTrue(transportSetting.GetTransportType() == TransportType.Amqp_WebSocket_Only, "Should be TransportType.Amqp_WebSocket_Only");
            Assert.IsTrue(transportSetting.PrefetchCount == 50, "Should be default value of 50");
        }

        [TestMethod]
        public void TransportSettingsTestTransportTypeAmqpWebSocketTcp()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, amqpConnectionPoolSettings);
            Assert.IsTrue(transportSetting.GetTransportType() == TransportType.Amqp_Tcp_Only, "Should be TransportType.Amqp_Tcp_Only");
            Assert.IsTrue(transportSetting.PrefetchCount == 200, "Should be value of 200");
        }

        [TestMethod]
        public void TransportSettingsTestTransportTypeHttp()
        {
            var transportSetting = new Http1TransportSettings();
            Assert.IsTrue(transportSetting.GetTransportType() == TransportType.Http1, "Should be TransportType.Http1");
        }

        [TestMethod]
        public void TransportSettingsTestTransportTypeMqttTcpOnly()
        {
            var transportSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            Assert.IsTrue(transportSetting.GetTransportType() == TransportType.Mqtt_Tcp_Only, "Should be TransportType.Mqtt_Tcp_Only");
        }

        [TestMethod]
        public void TransportSettingsTestTransportTypeMqttWebSocketOnly()
        {
            var transportSetting = new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only);
            Assert.IsTrue(transportSetting.GetTransportType() == TransportType.Mqtt_WebSocket_Only, "Should be TransportType.Mqtt_WebSocket_Only");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TransportSettingsTestTransportTypeMqtt()
        {
            new MqttTransportSettings(TransportType.Mqtt);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TransportSettingsTestZeroOperationTimeout()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp, 200, amqpConnectionPoolSettings);
            transportSetting.OperationTimeout = TimeSpan.Zero;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TransportSettingsTestZeroOpenTimeout()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp, 200, amqpConnectionPoolSettings);
            transportSetting.OpenTimeout = TimeSpan.Zero;
        }

        [TestMethod]
        public void TransportSettingsTestTimeouts()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 200, amqpConnectionPoolSettings);
            transportSetting.OpenTimeout = TimeSpan.FromMinutes(5);
            transportSetting.OperationTimeout = TimeSpan.FromMinutes(10);
            Assert.IsTrue(transportSetting.OpenTimeout == TimeSpan.FromMinutes(5), "OpenTimeout not set correctly");
            Assert.IsTrue(transportSetting.OperationTimeout == TimeSpan.FromMinutes(10), "OperationTimeout not set correctly");
        }

        [TestMethod]
        public void TransportSettingsTestDefaultTimeouts()
        {
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 200, amqpConnectionPoolSettings);
            Assert.IsTrue(transportSetting.OpenTimeout == TimeSpan.FromMinutes(1), "Default OpenTimeout not set correctly");
            Assert.IsTrue(transportSetting.OperationTimeout == TimeSpan.FromMinutes(1), "Default OperationTimeout not set correctly");

            var transportSetting2 = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 100);
            Assert.IsTrue(transportSetting2.OpenTimeout == TimeSpan.FromMinutes(1), "Default OpenTimeout not set correctly on transportSetting2");
            Assert.IsTrue(transportSetting2.OperationTimeout == TimeSpan.FromMinutes(1), "Default OperationTimeout not set correctly on transportSetting2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConnectionPoolSettingsTestZeroPoolSize()
        {
            var connectionPoolSettings = new AmqpConnectionPoolSettings();
            connectionPoolSettings.MaxPoolSize = 0;
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp, 200, connectionPoolSettings);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConnectionPoolSettingsTest4SecsIdleTimeout()
        {
            var connectionPoolSettings = new AmqpConnectionPoolSettings();
            connectionPoolSettings.ConnectionIdleTimeout = TimeSpan.FromSeconds(4);
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp, 200, connectionPoolSettings);
        }

        [TestMethod]
        public void ConnectionPoolSettingsTestMaxPoolSizeTest()
        {
            var connectionPoolSettings = new AmqpConnectionPoolSettings();
            connectionPoolSettings.MaxPoolSize = ushort.MaxValue;
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, connectionPoolSettings);
            Assert.IsTrue(transportSetting.AmqpConnectionPoolSettings.MaxPoolSize == ushort.MaxValue, "MaxPoolSize should be 64K");
        }

        [TestMethod]
        public void ConnectionPoolSettingsTestPoolingOff()
        {
            var connectionPoolSettings = new AmqpConnectionPoolSettings();
            connectionPoolSettings.Pooling = false;
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, connectionPoolSettings);
            Assert.IsTrue(transportSetting.AmqpConnectionPoolSettings.Pooling == false, "Pooling should be off");
        }

        [TestMethod]
        [Ignore]
        public void X509CertificateAmqpTransportSettingsTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 100) });
        }

        [TestMethod]
        [Ignore]
        public void X509CertificateHttp1TransportSettingsTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ITransportSettings[] { new Http1TransportSettings()});
        }

        [TestMethod]
        [Ignore]
        public void X509CertificateMqttTransportSettingsTest()
        {
            string hostName = "acme.azure-devices.net";
            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", cert);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ITransportSettings[]
            {
                new MqttTransportSettings(TransportType.Mqtt_Tcp_Only)
                {
                    ClientCertificate = cert,
                    RemoteCertificateValidationCallback = (a, b, c, d) => true
                },
                new MqttTransportSettings(TransportType.Mqtt_WebSocket_Only)
                {
                    ClientCertificate = cert,
                    RemoteCertificateValidationCallback = (a, b, c, d) => true
                }
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void NullX509CertificateAmqpTransportSettingsTest()
        {
            string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);

            var deviceClient = DeviceClient.Create(hostName, authMethod, new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 100) });
        }

        [TestMethod]
        [Ignore]
        public void X509CertificateMutipleClientAuthMechanism()
        {
            string hostName = "acme.azure-devices.net";
            var amqpConnectionPoolSettings = new AmqpConnectionPoolSettings();
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, amqpConnectionPoolSettings);
            var authMethod1 = new DeviceAuthenticationWithRegistrySymmetricKey("device1", "CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=");
            var deviceClient = DeviceClient.Create(hostName, authMethod1, new ITransportSettings[] { transportSetting });

            var cert = CertificateHelper.InstallCertificateFromFile(LocalCertFilename, LocalCertPasswordFile);
            var authMethod2 = new DeviceAuthenticationWithX509Certificate("device2", cert);
            var device2Client = DeviceClient.Create(hostName, authMethod2, new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 100) });
        }

        [TestMethod]
        public void AmqpTransportSettingsComparisonTests()
        {
            var amqpTransportSettings1 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpTransportSettings1.PrefetchCount = 100;
            amqpTransportSettings1.OpenTimeout = TimeSpan.FromMinutes(1);
            amqpTransportSettings1.OperationTimeout = TimeSpan.FromMinutes(1);

            Assert.IsTrue(amqpTransportSettings1.Equals(amqpTransportSettings1));
            Assert.IsFalse(amqpTransportSettings1.Equals(null));
            Assert.IsFalse(amqpTransportSettings1.Equals(new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)));

            var amqpTransportSettings2 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpTransportSettings2.PrefetchCount = 70;
            amqpTransportSettings2.OpenTimeout = TimeSpan.FromMinutes(1);
            amqpTransportSettings2.OperationTimeout = TimeSpan.FromMinutes(1);
            Assert.IsFalse(amqpTransportSettings1.Equals(amqpTransportSettings2));

            var amqpTransportSettings3 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpTransportSettings3.PrefetchCount = 100;
            amqpTransportSettings3.OpenTimeout = TimeSpan.FromMinutes(2);
            amqpTransportSettings3.OperationTimeout = TimeSpan.FromMinutes(1);
            Assert.IsFalse(amqpTransportSettings1.Equals(amqpTransportSettings3));

            var amqpTransportSettings4 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpTransportSettings4.PrefetchCount = 100;
            amqpTransportSettings4.OpenTimeout = TimeSpan.FromMinutes(1);
            amqpTransportSettings4.OperationTimeout = TimeSpan.FromMinutes(2);
            Assert.IsFalse(amqpTransportSettings1.Equals(amqpTransportSettings4));

            var amqpTransportSettings5 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpTransportSettings5.PrefetchCount = 100;
            amqpTransportSettings5.OpenTimeout = TimeSpan.FromMinutes(1);
            amqpTransportSettings5.OperationTimeout = TimeSpan.FromMinutes(1);
            Assert.IsTrue(amqpTransportSettings1.Equals(amqpTransportSettings5));
        }
    }
}