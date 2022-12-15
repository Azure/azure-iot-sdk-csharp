// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TransportSettingsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IotHubClientOptions_None_Throw()
        {
            var options = new IotHubClientOptions(null);
        }

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
        public void IotHubClientOptions_DefaultValues()
        {
            var options = new IotHubClientOptions();
            options.TransportSettings.Should().BeOfType(typeof(IotHubClientMqttSettings));
            options.FileUploadTransportSettings.Should().BeOfType(typeof(IotHubClientHttpSettings));
            options.PayloadConvention.Should().Be(DefaultPayloadConvention.Instance);
            options.SdkAssignsMessageId.Should().Be(SdkAssignsMessageId.Never);
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
            transportSetting.IdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
            transportSetting.SslProtocols.Should().Be(SslProtocols.None);
            transportSetting.ToString().Should().Be("IotHubClientAmqpSettings/Tcp");
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
            transportSetting.PublishToServerQoS.Should().Be(QualityOfService.AtLeastOnce);
            transportSetting.ReceivingQoS.Should().Be(QualityOfService.AtLeastOnce);
            transportSetting.IdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
            transportSetting.SslProtocols.Should().Be(SslProtocols.None);
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
        public async Task IotHubDeviceClient_NullX509Certificate()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(null, "device1");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings { PrefetchCount = 100 });

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IotHubDeviceClient_NullX509CertificateChain()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
            var authMethod = new ClientAuthenticationWithX509Certificate(cert, certificateChain: null, "device1");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings { PrefetchCount = 100 });

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task IotHubDeviceClient_NullX509Certificate_withChain()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
#pragma warning disable SYSLIB0026 // Type or member is obsolete
            using var cert = new X509Certificate2();
            var authMethod = new ClientAuthenticationWithX509Certificate(clientCertificate: null, certificateChain: null, "device1");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings { PrefetchCount = 100 });

            // act
            await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options);
        }

        [TestMethod]
        public void IotHubClientMqttSettings()
        {
            // arrange
            var settings = new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket)
            {
                IdleTimeout = TimeSpan.FromSeconds(1),
                PublishToServerQoS = QualityOfService.AtMostOnce,
                ReceivingQoS = QualityOfService.AtMostOnce,
                CleanSession = true,
                WebSocketKeepAlive = TimeSpan.FromSeconds(1),
                WillMessage = new WillMessage
                {
                    Payload = new byte[] { 1 },
                    QualityOfService = QualityOfService.AtMostOnce
                },
                AuthenticationChain = "chain"
            };
            var options = new IotHubClientOptions(settings)
            {
                GatewayHostName = "sampleHost",
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                FileUploadTransportSettings = new IotHubClientHttpSettings(),
                PayloadConvention = DefaultPayloadConvention.Instance,
                ModelId = "Id",
                AdditionalUserAgentInfo = "info"
            };

            // act
            var clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.GatewayHostName = "newHost";
            options.Should().NotBeEquivalentTo(clone);
        }

        [TestMethod]
        public void IotHubClientAmqpSettings()
        {
            // arrange
            var ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = 120,
                UsePooling = true,
            };
            var ConnectionPoolSettings_copy = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = 120,
                UsePooling = true,
            };
            var settings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)
            {
                IdleTimeout = TimeSpan.FromSeconds(1),
                WebSocketKeepAlive = TimeSpan.FromSeconds(1),
                AuthenticationChain = "chain",
                PrefetchCount = 10,
                ConnectionPoolSettings = ConnectionPoolSettings,
                ClientWebSocket = new ClientWebSocket(),
            };
            var options = new IotHubClientOptions(settings)
            {
                GatewayHostName = "sampleHost",
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
                FileUploadTransportSettings = new IotHubClientHttpSettings(),
                PayloadConvention = DefaultPayloadConvention.Instance,
                ModelId = "Id",
                AdditionalUserAgentInfo = "info"
            };

            // act
            var clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.GatewayHostName = "newHost";
            options.Should().NotBeEquivalentTo(clone);
            ConnectionPoolSettings.Equals(ConnectionPoolSettings_copy);
            ConnectionPoolSettings.Equals(null).Should().BeFalse();

            settings.ClientWebSocket.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IotHubClientAmqpPoolSettings()
        {
            var ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = 0,
                UsePooling = true,
            };
        }


        [TestMethod]
        public void IotHubClientNoRetry()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());

            // act
            var clone = options.Clone();

            // assert
            options.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();

            options.RetryPolicy = new IotHubClientNoRetry();
            options.RetryPolicy.Should().BeOfType<IotHubClientNoRetry>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
        }

        [TestMethod]
        public void IotHubClientIncrementalDelayRetryPolicy()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());

            // act
            var clone = options.Clone();

            // assert
            options.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();

            options.RetryPolicy = new IotHubClientIncrementalDelayRetryPolicy(0, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(100), false);
            options.RetryPolicy.Should().BeOfType<IotHubClientIncrementalDelayRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
        }

        [TestMethod]
        public void IotHubClientFixedDelayRetryPolicy()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());

            // act
            var clone = options.Clone();

            // assert
            options.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();

            options.RetryPolicy = new IotHubClientFixedDelayRetryPolicy(0, TimeSpan.FromSeconds(1), false);
            options.RetryPolicy.Should().BeOfType<IotHubClientFixedDelayRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
        }

        [TestMethod]
        public void IotHubClientCustomRetryPolicy()
        {
            // arrange
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings());

            // act
            var clone = options.Clone();

            // assert
            options.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();

            options.RetryPolicy = new CustomRetryPolicy();
            options.RetryPolicy.Should().BeOfType<CustomRetryPolicy>();
            clone.RetryPolicy.Should().BeOfType<IotHubClientExponentialBackoffRetryPolicy>();
        }

        internal class CustomRetryPolicy : IIotHubClientRetryPolicy
        {
            public CustomRetryPolicy() { }

            public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay)
            {
                throw new NotImplementedException();
            }
        }
    }
}
