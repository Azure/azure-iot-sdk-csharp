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
        public void AmqpTransportSettings_DefaultPropertyValues()
        {
            // arrange
            const TransportProtocol expectedProtocol = TransportProtocol.Tcp;
            const uint expectedPrefetchCount = 50;

            // act
            var transportSetting = new AmqpTransportSettings();

            // assert
            transportSetting.Protocol.Should().Be(expectedProtocol);
            transportSetting.PrefetchCount.Should().Be(expectedPrefetchCount);
        }

        [TestMethod]
        public void AmqpTransportSettings_RespectsCtorParameterTcp()
        {
            // arrange
            const TransportProtocol expectedProtocol = TransportProtocol.Tcp;

            // act
            var transportSetting = new AmqpTransportSettings(expectedProtocol);

            // assert
            transportSetting.Protocol.Should().Be(expectedProtocol);
        }

        [TestMethod]
        public void AmqpTransportSettings_RespectsCtorParameterWebSocket()
        {
            // arrange
            const TransportProtocol expectedProtocol = TransportProtocol.WebSocket;

            // act
            var transportSetting = new AmqpTransportSettings(expectedProtocol);

            // assert
            transportSetting.Protocol.Should().Be(expectedProtocol);
        }

        [TestMethod]
        public void MqttTransportSettings_DefaultPropertyValues()
        {
            // arrange
            const TransportProtocol expectedProtocol = TransportProtocol.Tcp;

            // act
            var transportSetting = new MqttTransportSettings();

            // assert
            transportSetting.Protocol.Should().Be(expectedProtocol);
        }

        [TestMethod]
        public void MqttTransportSettings_RespectsCtorParameterMqttTcp()
        {
            // arrange
            const TransportProtocol expectedTransportProtocol = TransportProtocol.Tcp;

            // act
            var transportSetting = new MqttTransportSettings(expectedTransportProtocol);

            // assert
            transportSetting.Protocol.Should().Be(expectedTransportProtocol);
        }

        [TestMethod]
        public void MqttTransportSettings_RespectsCtorParameterMqttWebSocket()
        {
            // arrange
            const TransportProtocol expectedTransportProtocol = TransportProtocol.WebSocket;

            // act
            var transportSetting = new MqttTransportSettings(expectedTransportProtocol);

            // assert
            transportSetting.Protocol.Should().Be(expectedTransportProtocol);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AmqpTransportSettings_UnderOperationTimeoutMin()
        {
            _ = new AmqpTransportSettings
            {
                OperationTimeout = TimeSpan.Zero,
            };
        }

        [TestMethod]
        public void AmqpTransportSettings_TimeoutPropertiesSet()
        {
            // arrange
            var tenMinutes = TimeSpan.FromMinutes(10);

            // act
            var transportSetting = new AmqpTransportSettings
            {
                OperationTimeout = tenMinutes,
            };

            // assert
            transportSetting.OperationTimeout.Should().Be(tenMinutes);
        }

        [TestMethod]
        public void AmqpTransportSettings_SetsDefaultTimeout()
        {
            // act
            var transportSetting = new AmqpTransportSettings();

            // assert
            transportSetting.OperationTimeout.Should().Be(AmqpTransportSettings.DefaultOperationTimeout, "Default OperationTimeout not set correctly");
            transportSetting.IdleTimeout.Should().Be(AmqpTransportSettings.DefaultIdleTimeout, "Default IdleTimeout not set correctly");
            transportSetting.DefaultReceiveTimeout.Should().Be(AmqpTransportSettings.DefaultOperationTimeout, "Default DefaultReceiveTimeout not set correctly");
        }

        [TestMethod]
        public void AmqpTransportSettings_OverridesDefaultTimeout()
        {
            // We want to test that the timeouts that we set on AmqpTransportSettings override the default timeouts.
            // In order to test that, we need to ensure the test timeout values are different from the default timeout values.
            // Adding a TimeSpan to the default timeout value is an easy way to achieve that.
            var expectedOperationTimeout = AmqpTransportSettings.DefaultOperationTimeout.Add(TimeSpan.FromMinutes(5));
            var expectedIdleTimeout = AmqpTransportSettings.DefaultIdleTimeout.Add(TimeSpan.FromMinutes(5));

            // act
            var transportSetting = new AmqpTransportSettings
            {
                OperationTimeout = expectedOperationTimeout,
                IdleTimeout = expectedIdleTimeout,
            };

            // assert
            transportSetting.OperationTimeout.Should().Be(expectedOperationTimeout);
            transportSetting.IdleTimeout.Should().Be(expectedIdleTimeout);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
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
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AmqpConnectionPoolSettings_OverMaxPoolSize()
        {
            _ = new AmqpConnectionPoolSettings { MaxPoolSize = AmqpConnectionPoolSettings.AbsoluteMaxPoolSize + 1 };
        }

        [TestMethod]
        public void ConnectionPoolSettings_PoolingOff()
        {
            // act
            var connectionPoolSettings = new AmqpConnectionPoolSettings { Pooling = false };

            // assert
            connectionPoolSettings.Pooling.Should().BeFalse();
        }

        [TestMethod]
        public void AmqpTransportSettings_Equals()
        {
            // act
            var amqpTransportSettings1 = new AmqpTransportSettings
            {
                PrefetchCount = 100,
                OperationTimeout = TimeSpan.FromMinutes(1),
            };
            // different prefetch
            var amqpTransportSettings2 = new AmqpTransportSettings
            {
                PrefetchCount = amqpTransportSettings1.PrefetchCount + 1,
                OperationTimeout = amqpTransportSettings1.OperationTimeout,
            };
            // different operation timeout
            var amqpTransportSettings3 = new AmqpTransportSettings
            {
                PrefetchCount = amqpTransportSettings1.PrefetchCount,
                OperationTimeout = amqpTransportSettings1.OperationTimeout.Add(TimeSpan.FromMinutes(1)),
            };
            // same
            var amqpTransportSettings4 = new AmqpTransportSettings
            {
                PrefetchCount = amqpTransportSettings1.PrefetchCount,
                OperationTimeout = amqpTransportSettings1.OperationTimeout,
            };

            // assert
            amqpTransportSettings1.Should().Be(amqpTransportSettings1, "An object should equal itself");
            amqpTransportSettings1.Should().NotBe(new AmqpTransportSettings());
            amqpTransportSettings1.Should().NotBe(amqpTransportSettings2);
            amqpTransportSettings1.Should().NotBe(amqpTransportSettings3);
            amqpTransportSettings1.Should().Be(amqpTransportSettings4);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClient_NullX509Certificate()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);
            var options = new ClientOptions(new AmqpTransportSettings { PrefetchCount = 100 });

            // act
            _ = DeviceClient.Create(hostName, authMethod, options);
        }
    }
}
