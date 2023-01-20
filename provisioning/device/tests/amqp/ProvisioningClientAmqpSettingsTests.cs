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
    public class ProvisioningClientAmqpSettingsTests
    {
        [TestMethod]
        public void ProvisioningClientAmqpSettings_Default()
        {
            // arrange - act
            var amqpSettings = new ProvisioningClientAmqpSettings();

            // assert
            amqpSettings.IdleTimeout.Should().Be(TimeSpan.FromMinutes(2));
        }

        [TestMethod]
        public void ProvisioningClientAmqpSettings_Clone()
        {
            // arrange
            var amqpSettings = new ProvisioningClientAmqpSettings(ProvisioningClientTransportProtocol.Tcp)
            {
                SslProtocols = SslProtocols.Tls12,
                IdleTimeout = TimeSpan.FromSeconds(1),
                CertificateRevocationCheck = true,
            };

            // act
            ProvisioningClientTransportSettings clone = amqpSettings.Clone();

            // assert
            clone.Should().BeOfType<ProvisioningClientAmqpSettings>();
            amqpSettings.Should().NotBeSameAs(clone);
            amqpSettings.Should().BeEquivalentTo(clone);

            amqpSettings.CertificateRevocationCheck = false;
            amqpSettings.Should().NotBeEquivalentTo(clone);
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
