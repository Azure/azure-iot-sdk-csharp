// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class IotHubDeviceClientOptionsCloneTest
    {
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
            var settings = new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket)
            {
                IdleTimeout = TimeSpan.FromSeconds(1),
                WebSocketKeepAlive = TimeSpan.FromSeconds(1),
                AuthenticationChain = "chain",
                PrefetchCount = 10,
                ConnectionPoolSettings = new AmqpConnectionPoolSettings
                {
                    MaxPoolSize = 120,
                    UsePooling = true,
                },
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

            settings.ClientWebSocket.Dispose();
        }
    }
}
