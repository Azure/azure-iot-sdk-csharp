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
        [DataRow(TransportProtocol.Tcp)]
        [DataRow(TransportProtocol.WebSocket)]
        public void AmqpTransportSettings_RespectsCtorParameter(TransportProtocol protocol)
        {
            // act
            var transportSetting = new AmqpTransportSettings(protocol);

            // assert
            transportSetting.Protocol.Should().Be(protocol);
        }

        [TestMethod]
        public void MqttTransportSettings_DefaultPropertyValues()
        {
            // act
            var transportSetting = new MqttTransportSettings();

            // assert
            transportSetting.Protocol.Should().Be(TransportProtocol.Tcp);
        }

        [TestMethod]
        [DataRow(TransportProtocol.Tcp)]
        [DataRow(TransportProtocol.WebSocket)]
        public void MqttTransportSettings_RespectsCtorParameter(TransportProtocol protocol)
        {
            // act
            var transportSetting = new MqttTransportSettings(protocol);

            // assert
            transportSetting.Protocol.Should().Be(protocol);
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
        [ExpectedException(typeof(ArgumentException))]
        public void IotHubDeviceClient_NullX509Certificate()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);
            var options = new IotHubClientOptions(new AmqpTransportSettings { PrefetchCount = 100 });

            // act
            _ = IotHubDeviceClient.Create(hostName, authMethod, options);
        }
    }
}
