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
            // arrange
            var amqpSettings = new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
            };
            var options = new ProvisioningClientOptions(amqpSettings)
            {
                AdditionalUserAgentInfo = "info"
            };

            // act
            var clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.AdditionalUserAgentInfo = "updated";
            options.Should().NotBeEquivalentTo(clone);
        }

        [TestMethod]
        public void ProvisioningClientMqttSettings()
        {
            // arrange
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

            // act
            var clone = options.Clone();

            // assert
            options.Should().NotBeSameAs(clone);
            options.Should().BeEquivalentTo(clone);

            options.AdditionalUserAgentInfo = "updated";
            options.Should().NotBeEquivalentTo(clone);
        }
    }
}
