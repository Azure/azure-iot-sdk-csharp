// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Azure.Devices.Provisioning.Client.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class CertificateInstallerTests
    {
        [TestMethod]
        public void CertificateInstaller_EnsureChainIsInstalled_Works()
        {
            // arrange

            using var ecdsa = ECDsa.Create();
            var request = new CertificateRequest("CN=testSubject", ecdsa, HashAlgorithmName.SHA256);
            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1));

            var mockCertCollection = new Mock<X509Certificate2Collection>();
            mockCertCollection.Object.Add(cert);

            var certStore = new Mock<ICertificateStore>();

            // act
            CertificateInstaller.EnsureChainIsInstalled(mockCertCollection.Object, certStore.Object);

            // assert
            certStore.Verify(p => p.Add(It.IsAny<X509Certificate2>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void CertificateInstaller_EnsureChainIsInstalled_NullCert()
        {
            // arrange

            var mockCert = new Mock<X509Certificate>();

            var mockCertCollection = new Mock<X509Certificate2Collection>();
            mockCertCollection.Object.Add(mockCert.Object);

            var certStore = new Mock<ICertificateStore>();

            // act
            CertificateInstaller.EnsureChainIsInstalled(mockCertCollection.Object, certStore.Object);

            // assert
            certStore.Verify(p => p.Add(It.IsAny<X509Certificate2>()), Times.Never);
        }
    }
}
