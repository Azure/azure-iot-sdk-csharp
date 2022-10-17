// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TransportSettingsTests
    {
        private const string LocalCertFilename = "..\\..\\Microsoft.Azure.Devices.Client.Test\\LocalNoChain.pfx";
        private const string LocalCertPasswordFile = "..\\..\\Microsoft.Azure.Devices.Client.Test\\TestCertsPassword.txt";

        [TestMethod]
        public void IotHubClientOptions_Mqtt_DoesNotThrow()
        {
            // should not throw
            var options = new IotHubClientOptions(new IotHubClientMqttSettings());
        }

        [TestMethod]
        public void IotHubClientOptions_Amqp_DoesNotThrow()
        {
            // should not throw
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubClientOptions_Http_Throws()
        {
            var options = new IotHubClientOptions(new IotHubClientHttpSettings());
        }

        [TestMethod]
        public void AmqpTransportSettings_DefaultPropertyValues()
        {
            // arrange
            const IotHubClientTransportProtocol expectedProtocol = IotHubClientTransportProtocol.Tcp;
            const uint expectedPrefetchCount = 50;

            // act
            var transportSetting = new IotHubClientAmqpSettings();

            // assert
            transportSetting.Protocol.Should().Be(expectedProtocol);
            transportSetting.PrefetchCount.Should().Be(expectedPrefetchCount);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public void AmqpTransportSettings_RespectsCtorParameter(IotHubClientTransportProtocol protocol)
        {
            // act
            var transportSetting = new IotHubClientAmqpSettings(protocol);

            // assert
            transportSetting.Protocol.Should().Be(protocol);
        }

        [TestMethod]
        public void MqttTransportSettings_DefaultPropertyValues()
        {
            // act
            var transportSetting = new IotHubClientMqttSettings();

            // assert
            transportSetting.Protocol.Should().Be(IotHubClientTransportProtocol.Tcp);
        }

        [TestMethod]
        [DataRow(IotHubClientTransportProtocol.Tcp)]
        [DataRow(IotHubClientTransportProtocol.WebSocket)]
        public void MqttTransportSettings_RespectsCtorParameter(IotHubClientTransportProtocol protocol)
        {
            // act
            var transportSetting = new IotHubClientMqttSettings(protocol);

            // assert
            transportSetting.Protocol.Should().Be(protocol);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AmqpConnectionPoolSettings_UnderMinPoolSize()
        {
            _ = new AmqpConnectionPoolSettings { MaxPoolSize = 0 };
        }

        [TestMethod]
        public void AmqpConnectionPoolSettings_MaxPoolSizeTest()
        {
            // arrange
            const uint maxPoolSize = AmqpConnectionPoolSettings.AbsoluteMaxPoolSize;

            // act
            var connectionPoolSettings = new AmqpConnectionPoolSettings { MaxPoolSize = maxPoolSize };

            // assert
            Assert.AreEqual(maxPoolSize, connectionPoolSettings.MaxPoolSize, "Should match initialized value");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AmqpConnectionPoolSettings_OverMaxPoolSize()
        {
            _ = new AmqpConnectionPoolSettings { MaxPoolSize = AmqpConnectionPoolSettings.AbsoluteMaxPoolSize + 1 };
        }

        [TestMethod]
        public void ConnectionPoolSettings_PoolingOff()
        {
            // act
            var connectionPoolSettings = new AmqpConnectionPoolSettings { UsePooling = false };

            // assert
            connectionPoolSettings.UsePooling.Should().BeFalse();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubDeviceClient_NullX509Certificate()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(null, "device1");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings { PrefetchCount = 100 });

            // act
            using var dc = new IotHubDeviceClient(hostName, authMethod, options);
        }
    }
}
