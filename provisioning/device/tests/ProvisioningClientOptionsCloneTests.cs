// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Security.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientOptionsCloneTests
    {
        [TestMethod]
        public void ProvisioningClientAmqpSettings()
        {
            var amqpSettings = new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
            };
            var options = new ProvisioningClientOptions(amqpSettings)
            {
                AdditionalUserAgentInfo = "info"
            };
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.AdditionalUserAgentInfo.Should().BeEquivalentTo(clone.AdditionalUserAgentInfo);
            options.TransportSettings.Should().NotBeSameAs(clone.TransportSettings);
            options.TransportSettings.Should().BeEquivalentTo(clone.TransportSettings);

        }

        [TestMethod]
        public void ProvisioningClientMqttSettings()
        {
            var mqttSettings = new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
                PublishToServerQoS = QualityOfService.AtMostOnce,
            };
            var options = new ProvisioningClientOptions(mqttSettings)
            {
                AdditionalUserAgentInfo = "info"
            };
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.AdditionalUserAgentInfo.Should().BeEquivalentTo(clone.AdditionalUserAgentInfo);
            options.TransportSettings.Should().NotBeSameAs(clone.TransportSettings);
            options.TransportSettings.Should().BeEquivalentTo(clone.TransportSettings);
        }

        [TestMethod]
        public void ProvisioningClientHttpSettings()
        {
            var httpSettings = new ProvisioningClientHttpSettings()
            {
                SslProtocols = SslProtocols.Tls12,
            };
            var options = new ProvisioningClientOptions(httpSettings)
            {
                AdditionalUserAgentInfo = "info"
            };
            var clone = options.Clone();

            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);
            options.AdditionalUserAgentInfo.Should().BeEquivalentTo(clone.AdditionalUserAgentInfo);
            options.TransportSettings.Should().NotBeSameAs(clone.TransportSettings);
            options.TransportSettings.Should().BeEquivalentTo(clone.TransportSettings);
        }
    }
}
