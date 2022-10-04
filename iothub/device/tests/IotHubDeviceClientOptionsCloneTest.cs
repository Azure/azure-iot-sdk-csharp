// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.TransportSettings.Should().NotBeSameAs(clone.TransportSettings);
            options.TransportSettings.Should().BeEquivalentTo(clone.TransportSettings);
            options.GatewayHostName.Should().BeEquivalentTo(clone.GatewayHostName);
            options.SdkAssignsMessageId.Should().NotBeSameAs(clone.SdkAssignsMessageId);
            options.SdkAssignsMessageId.Should().BeEquivalentTo(clone.SdkAssignsMessageId);
            options.FileUploadTransportSettings.Should().NotBeSameAs(clone.FileUploadTransportSettings);
            options.FileUploadTransportSettings.Should().BeEquivalentTo(clone.FileUploadTransportSettings);
            options.PayloadConvention.Should().BeEquivalentTo(clone.PayloadConvention);
            options.ModelId.Should().BeEquivalentTo(clone.ModelId);
            options.AdditionalUserAgentInfo.Should().BeEquivalentTo(clone.AdditionalUserAgentInfo);
        }

        [TestMethod]
        public void IotHubClientAmqpSettings()
        {
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
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.TransportSettings.Should().NotBeSameAs(clone.TransportSettings);
            options.TransportSettings.Should().BeEquivalentTo(clone.TransportSettings);
            options.GatewayHostName.Should().BeEquivalentTo(clone.GatewayHostName);
            options.SdkAssignsMessageId.Should().NotBeSameAs(clone.SdkAssignsMessageId);
            options.SdkAssignsMessageId.Should().BeEquivalentTo(clone.SdkAssignsMessageId);
            options.FileUploadTransportSettings.Should().NotBeSameAs(clone.FileUploadTransportSettings);
            options.FileUploadTransportSettings.Should().BeEquivalentTo(clone.FileUploadTransportSettings);
            options.PayloadConvention.Should().BeEquivalentTo(clone.PayloadConvention);
            options.ModelId.Should().BeEquivalentTo(clone.ModelId);
            options.AdditionalUserAgentInfo.Should().BeEquivalentTo(clone.AdditionalUserAgentInfo);
        }
    }
}
