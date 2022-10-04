// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            var options = new ProvisioningClientOptions(amqpSettings);
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
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
            var options = new ProvisioningClientOptions(mqttSettings);
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
        }

        [TestMethod]
        public void ProvisioningClientHttpSettings()
        {
            var httpSettings = new ProvisioningClientHttpSettings()
            {
                SslProtocols = SslProtocols.Tls12,
            };
            var options = new ProvisioningClientOptions(httpSettings);
            var clone = options.Clone();

            Assert.AreNotSame(options, clone);
        }
    }
}
