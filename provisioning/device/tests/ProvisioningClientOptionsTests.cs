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
    public class ProvisioningClientOptionsTests
    {
        [TestMethod]
        public void ProvisioningClient_DefaultTransportSettings()
        {
            // arrange - act
            var options = new ProvisioningClientOptions();

            // assert
            options.TransportSettings.Should().BeOfType<ProvisioningClientMqttSettings>();
        }

        [TestMethod]
        public void ProvisioningClient_Clone_AmqpSettings()
        {
            // arrange
            var amqpSettings = new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
                CertificateRevocationCheck = true,
            };
            var options = new ProvisioningClientOptions(amqpSettings)
            {
                AdditionalUserAgentInfo = "info"
            };

            // act
            ProvisioningClientOptions clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.AdditionalUserAgentInfo = "updated";
            options.Should().NotBeEquivalentTo(clone);
        }

        [TestMethod]
        public void ProvisioningClient_Clone_MqttSettings()
        {
            // arrange
            var mqttSettings = new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
                PublishToServerQoS = QualityOfService.AtMostOnce,
                CertificateRevocationCheck = true,
            };
            var options = new ProvisioningClientOptions(mqttSettings)
            {
                AdditionalUserAgentInfo = "info"
            };

            // act
            ProvisioningClientOptions clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.AdditionalUserAgentInfo = "updated";
            options.Should().NotBeEquivalentTo(clone);
        }
    }
}
