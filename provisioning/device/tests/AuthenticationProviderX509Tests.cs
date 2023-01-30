using Microsoft.Azure.Devices.Provisioning.Client;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AuthenticationProviderX509Tests
    {
#pragma warning disable SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2 s_cert = new();
#pragma warning restore SYSLIB0026 // Type or member is obsolete
        private static readonly X509Certificate2Collection s_certs = new();

        [TestMethod]
        public void AuthenticationProviderX509_ThrowsWhenMissingCert()
        {
            // arrange - act
            Func<AuthenticationProviderX509> act = () => new AuthenticationProviderX509(null);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AuthenticationProviderX509_Works()
        {
            // arrange - act
            var authProvider = new AuthenticationProviderX509(s_cert, s_certs);

            // assert
            authProvider.Should().NotBeNull();
            authProvider.ClientCertificate.Should().Be(s_cert);
            authProvider.CertificateChain.Should().BeEquivalentTo(s_certs);
            authProvider.GetRegistrationId().Should().Be(s_cert.GetNameInfo(X509NameType.DnsName, false)); // TODO: fix or remove
        }
    }
}
