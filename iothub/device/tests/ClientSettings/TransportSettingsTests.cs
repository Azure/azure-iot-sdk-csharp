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
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AmqpTransportSettings_InvalidTransportTypeAmqpHttp()
        {
            _ = new AmqpTransportSettings(TransportType.Http1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AmqpTransportSettings_UnderPrefetchCountMin()
        {
            _ = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 0, new AmqpConnectionPoolSettings());
        }

        [TestMethod]
        public void AmqpTransportSettings_DefaultPropertyValues()
        {
            // arrange
            const TransportType transportType = TransportType.Amqp_WebSocket_Only;
            const uint prefetchCount = 50;

            // act
            var transportSetting = new AmqpTransportSettings(transportType);

            // assert
            Assert.AreEqual(transportType, transportSetting.GetTransportType(), "Should match initialized value");
            Assert.AreEqual(prefetchCount, transportSetting.PrefetchCount, "Should default to 50");
        }

        [TestMethod]
        public void AmqpTransportSettings_RespectsCtorParameters()
        {
            // arrange
            const TransportType transportType = TransportType.Amqp_Tcp_Only;
            const uint prefetchCount = 200;

            // act
            var transportSetting = new AmqpTransportSettings(transportType, prefetchCount, new AmqpConnectionPoolSettings());

            // assert
            Assert.AreEqual(transportType, transportSetting.GetTransportType(), "Should match initialized value");
            Assert.AreEqual(prefetchCount, transportSetting.PrefetchCount, "Should match initialized value");
        }

        [TestMethod]
        public void Http1TransportSettings_DefaultTransportType()
        {
            Assert.AreEqual(TransportType.Http1, new HttpTransportSettings().GetTransportType(), "Should default to TransportType.Http1");
        }

        [TestMethod]
        public void MqttTransportSettings_RespectsCtorParameterMqttTcpOnly()
        {
            // arrange
            const TransportType transportType = TransportType.Mqtt_Tcp_Only;

            // act
            var transportSetting = new MqttTransportSettings(transportType);

            // assert
            Assert.AreEqual(transportType, transportSetting.GetTransportType(), "Should match initilized value");
        }

        [TestMethod]
        public void MqttTransportSettings_RespectsCtorParameterMqttWebSocketOnly()
        {
            // arrange
            const TransportType transportType = TransportType.Mqtt_WebSocket_Only;

            // act
            var transportSetting = new MqttTransportSettings(transportType);

            // assert
            Assert.AreEqual(transportType, transportSetting.GetTransportType(), "Should match initilized value");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AmqpTransportSettings_UnderOperationTimeoutMin()
        {
            _ = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, new AmqpConnectionPoolSettings())
            {
                OperationTimeout = TimeSpan.Zero,
            };
        }

        [TestMethod]
        public void AmqpTransportSettings_TimeoutPropertiesSet()
        {
            // arrange
            var fiveMinutes = TimeSpan.FromMinutes(5);
            var tenMinutes = TimeSpan.FromMinutes(10);

            // act
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 200, new AmqpConnectionPoolSettings())
            {
                OperationTimeout = tenMinutes,
            };

            // assert
            Assert.AreEqual(tenMinutes, transportSetting.OperationTimeout, "Should match initialized value");
        }

        [TestMethod]
        public void AmqpTransportSettings_SetsDefaultTimeout()
        {
            // act
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 200);

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
            var openTimeout = AmqpTransportSettings.DefaultOpenTimeout.Add(TimeSpan.FromMinutes(5));
            var operationTimeout = AmqpTransportSettings.DefaultOperationTimeout.Add(TimeSpan.FromMinutes(5));
            var idleTimeout = AmqpTransportSettings.DefaultIdleTimeout.Add(TimeSpan.FromMinutes(5));

            // act
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only, 200)
            {
                OperationTimeout = operationTimeout,
                IdleTimeout = idleTimeout,
            };

            // assert
            Assert.AreEqual(operationTimeout, transportSetting.OperationTimeout, "OperationTimeout not set correctly");
            Assert.AreEqual(idleTimeout, transportSetting.IdleTimeout, "IdleTimeout not set correctly");
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
            var transportSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 200, new AmqpConnectionPoolSettings { Pooling = false });

            // assert
            Assert.IsFalse(transportSetting.AmqpConnectionPoolSettings.Pooling, "Should match initialized value");
        }

        [TestMethod]
        public void AmqpTransportSettings_Equals()
        {
            // act
            var amqpTransportSettings1 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
            {
                PrefetchCount = 100,
                OperationTimeout = TimeSpan.FromMinutes(1),
            };
            var amqpTransportSettings2 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
            {
                PrefetchCount = 70,
                OperationTimeout = TimeSpan.FromMinutes(1),
            };
            var amqpTransportSettings3 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
            {
                PrefetchCount = 100,
                OperationTimeout = TimeSpan.FromMinutes(2),
            };
            var amqpTransportSettings4 = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
            {
                PrefetchCount = 100,
                OperationTimeout = TimeSpan.FromMinutes(1),
            };

            // assert
            Assert.IsTrue(amqpTransportSettings1.Equals(amqpTransportSettings1), "An object should equal itself");
            Assert.IsFalse(amqpTransportSettings1.Equals(null), "An instantiated object is not");
            Assert.IsFalse(amqpTransportSettings1.Equals(new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)));
            Assert.IsFalse(amqpTransportSettings1.Equals(amqpTransportSettings2));
            Assert.IsFalse(amqpTransportSettings1.Equals(amqpTransportSettings3));
            Assert.IsTrue(amqpTransportSettings1.Equals(amqpTransportSettings4));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClient_NullX509Certificate()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new DeviceAuthenticationWithX509Certificate("device1", null);

            // act
            _ = DeviceClient.Create(hostName, authMethod, new AmqpTransportSettings(TransportType.Amqp_Tcp_Only, 100));
        }
    }
}
