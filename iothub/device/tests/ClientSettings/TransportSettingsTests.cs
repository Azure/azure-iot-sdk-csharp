// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
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
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete

        [TestMethod]
        public void IotHubClientOptions_None_Throw()
        {       
            Action act = () => _ = new IotHubClientOptions(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void IotHubClientOptions_Mqtt_DoesNotThrow()
        {
            Action act = () => _ = new IotHubClientOptions(new IotHubClientMqttSettings());
            act.Should().NotThrow();
        }

        [TestMethod]
        public void IotHubClientOptions_Amqp_DoesNotThrow()
        {
            Action act = () => _ = new IotHubClientOptions(new IotHubClientAmqpSettings());
            act.Should().NotThrow();
        }

        [TestMethod]
        public void IotHubClientOptions_Http_Throws()
        {
            Action act = () => _ = new IotHubClientOptions(new IotHubClientHttpSettings());
            act.Should().Throw<ArgumentException>();
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
            var transportSetting = new IotHubClientAmqpSettings(protocol);
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
            var transportSetting = new IotHubClientMqttSettings(protocol);
            transportSetting.Protocol.Should().Be(protocol);
        }

        [TestMethod]
        public void IotHubDeviceClient_NullX509Certificate()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(null, "device1");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task IotHubDeviceClient_FileUploadTransport_Proxy()
        {
            // arrange
            const string hostName = "acme.azure-devices.net";
            var authMethod = new ClientAuthenticationWithX509Certificate(s_cert, "device1");
            var options = new IotHubClientOptions(new IotHubClientAmqpSettings { PrefetchCount = 100 });
            options.FileUploadTransportSettings = new IotHubClientHttpSettings()
            {
                Proxy = new WebProxy(),
            };

            // act
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(hostName, authMethod, options); };

            // assert
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public void IotHubDeviceClient_NullX509CertificateChain()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(s_cert, certificateChain: null, "device1");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void IotHubDeviceClient_NullX509Certificate_withChain()
        {
            Action act = () => _ = new ClientAuthenticationWithX509Certificate(null, certificateChain: null, "device1");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void IotHubClientMqttSettings_Clone()
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
        
            settings.WillMessage.Payload.Should().NotBeNull();
            settings.WillMessage.QualityOfService.Should().Be(QualityOfService.AtMostOnce);
        }

        [TestMethod]
        public void IotHubClientAmqpSettings_Clone()
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
        public void IotHubClientAmqpPoolSettings_ArgumentOutOfRange_Throws()
        {
            Action act = () => _ = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = 0,
                UsePooling = true,
            };
            act.Should().Throw<ArgumentOutOfRangeException>();
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
