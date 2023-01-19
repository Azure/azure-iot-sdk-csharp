// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Security.Authentication;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningClientMqttSettingsTests
    {
        [TestMethod]
        public void ProvisioningClientMqttSettings_Default()
        {
            // arrange - act
            var mqttSettings = new ProvisioningClientMqttSettings();

            // assert
            mqttSettings.PublishToServerQoS.Should().Be(QualityOfService.AtLeastOnce);
            mqttSettings.ReceivingQoS.Should().Be(QualityOfService.AtLeastOnce);
            mqttSettings.IdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
        }

        [TestMethod]
        public void ProvisioningClientMqttSettings_Clone()
        {
            // arrange
            var mqttSettings = new ProvisioningClientMqttSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
                PublishToServerQoS = QualityOfService.AtMostOnce,
                CertificateRevocationCheck = true,
            };

            // act
            ProvisioningClientTransportSettings clone = mqttSettings.Clone();

            // assert
            clone.Should().BeOfType<ProvisioningClientMqttSettings>();
            mqttSettings.Should().NotBeSameAs(clone);
            mqttSettings.Should().BeEquivalentTo(clone);

            mqttSettings.CertificateRevocationCheck = false;
            mqttSettings.Should().NotBeEquivalentTo(clone);
        }

        [TestMethod]
        [DataRow(SslPolicyErrors.None)]
        [DataRow(SslPolicyErrors.RemoteCertificateNotAvailable)]
        [DataRow(SslPolicyErrors.RemoteCertificateNameMismatch)]
        [DataRow(SslPolicyErrors.RemoteCertificateChainErrors)]
        public void DefaultRemoteCertificateValidation_DifferentSslPolicyErrors(SslPolicyErrors sslPolicyErrors)
        {
            // arrange - act
            var validation = ProvisioningClientTransportSettings.DefaultRemoteCertificateValidation("sender", null, null, sslPolicyErrors);

            // assert
            validation.Should().Be(sslPolicyErrors == SslPolicyErrors.None);
        }
    }
}
