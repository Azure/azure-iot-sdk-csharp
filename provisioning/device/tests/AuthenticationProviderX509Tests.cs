// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AuthenticationProviderX509Tests
    {
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
            // arrange

            using var ecdsa = ECDsa.Create();
            var request = new CertificateRequest("CN=testSubject", ecdsa, HashAlgorithmName.SHA256);
            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1));

            // act
            var authProvider = new AuthenticationProviderX509(cert, s_certs);

            // assert

            authProvider.Should().NotBeNull();
            authProvider.ClientCertificate.Should().Be(cert);
            authProvider.CertificateChain.Should().BeEquivalentTo(s_certs);
            authProvider.GetRegistrationId().Should().Be(cert.GetNameInfo(X509NameType.DnsName, false));
        }
    }
}
