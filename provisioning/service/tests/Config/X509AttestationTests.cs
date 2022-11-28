// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509AttestationTests
    {
        private const string PublicKeyCertificateString =
            "-----BEGIN CERTIFICATE-----\n" +
            "MIIBiDCCAS2gAwIBAgIFWks8LR4wCgYIKoZIzj0EAwIwNjEUMBIGA1UEAwwLcmlv\n" +
            "dGNvcmVuZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzAgFw0xNzAx\n" +
            "MDEwMDAwMDBaGA8zNzAxMDEzMTIzNTk1OVowNjEUMBIGA1UEAwwLcmlvdGNvcmVu\n" +
            "ZXcxETAPBgNVBAoMCE1TUl9URVNUMQswCQYDVQQGEwJVUzBZMBMGByqGSM49AgEG\n" +
            "CCqGSM49AwEHA0IABLVS6bK+QMm+HZ0247Nm+JmnERuickBXTj6rydcP3WzVQNBN\n" +
            "pvcQ/4YVrPp60oiYRxZbsPyBtHt2UCAC00vEXy+jJjAkMA4GA1UdDwEB/wQEAwIH\n" +
            "gDASBgNVHRMBAf8ECDAGAQH/AgECMAoGCCqGSM49BAMCA0kAMEYCIQDEjs2PoZEi\n" +
            "/yAQNj2Vji9RthQ33HG/QdL12b1ABU5UXgIhAPJujG/c/S+7vcREWI7bQcCb31JI\n" +
            "BDhWZbt4eyCvXZtZ\n" +
            "-----END CERTIFICATE-----\n";
        private const string CaReferenceString = "valid-ca-reference";

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            X509Certificate2 nullCert = null;
            string nullString = null;

            // act - assert
            var acts = new Action[]
            {
                () => _ = X509Attestation.CreateFromClientCertificates(nullCert),
                () => _ = X509Attestation.CreateFromClientCertificates(nullCert, nullCert),
                () => _ = X509Attestation.CreateFromClientCertificates(nullString),
                () => _ = X509Attestation.CreateFromClientCertificates(nullString, nullString),
            };
            foreach (Action act in acts)
            {
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesThrowsOnInvalidPrimaryCertificate()
        {
            // arrange
            string primaryStr =
                "-----BEGIN CERTIFICATE-----\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxx\n" +
                "-----END CERTIFICATE-----\n";
            string secondaryStr = null;

            // act - assert
            Action act1 = () => X509Attestation.CreateFromClientCertificates(primaryStr);
            var error1 = act1.Should().Throw<ProvisioningServiceException>();
            error1.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error1.And.IsTransient.Should().BeFalse();

            Action act2 = () => X509Attestation.CreateFromClientCertificates(primaryStr, secondaryStr);
            var error2 = act2.Should().Throw<ProvisioningServiceException>();
            error2.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error2.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            X509Certificate2 nullCert = null;
            string nullString = null;

            var acts = new Action[]
            {
                () => _ = X509Attestation.CreateFromRootCertificates(nullCert),
                () => _ = X509Attestation.CreateFromRootCertificates(nullCert, nullCert),
                () => _ = X509Attestation.CreateFromRootCertificates(nullString),
                () => _ = X509Attestation.CreateFromRootCertificates(nullString, nullString),
            };

            foreach (Action act in acts)
            {
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesThrowsOnInvalidPrimaryCertificate()
        {
            // arrange
            string primaryStr =
                "-----BEGIN CERTIFICATE-----\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n" +
                "xxxxxxxxxxxxxxxx\n" +
                "-----END CERTIFICATE-----\n";
            string secondaryStr = null;

            // act - assert
            Action act1 = () => X509Attestation.CreateFromRootCertificates(primaryStr);
            var error1 = act1.Should().Throw<ProvisioningServiceException>();
            error1.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error1.And.IsTransient.Should().BeFalse();

            Action act2 = () => X509Attestation.CreateFromRootCertificates(primaryStr, secondaryStr);
            var error2 = act2.Should().Throw<ProvisioningServiceException>();
            error2.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error2.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            using var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNotNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromClientCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromClientCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.ClientCertificates.Primary);
            Assert.IsNull(attestation.ClientCertificates.Secondary);
            Assert.IsNull(attestation.RootCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryCertificate()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryCertificates()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            using var secondary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullCertificates()
        {
            // arrange
            using var primary = new X509Certificate2(Encoding.ASCII.GetBytes(PublicKeyCertificateString));
            X509Certificate2 secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = PublicKeyCertificateString;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNotNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromRootCertificatesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = PublicKeyCertificateString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromRootCertificates(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.RootCertificates.Primary);
            Assert.IsNull(attestation.RootCertificates.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.CaReferences);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesThrowsOnNullPrimaryCertificate()
        {
            // arrange
            string nullString = null;

            // act - assert
            var acts = new Action[]
            {
                () => _ = X509Attestation.CreateFromCaReferences(nullString),
                () => _ = X509Attestation.CreateFromCaReferences(nullString, nullString),
            };

            foreach (var act in acts)
            {
                act.Should().Throw<ArgumentNullException>();
            }
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryString()
        {
            // arrange
            string primary = CaReferenceString;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryString()
        {
            // arrange
            string primary = CaReferenceString;
            string secondary = CaReferenceString;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNotNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }

        [TestMethod]
        public void X509AttestationCreateFromCAReferencesSucceedOnPrimaryAndSecondaryNullString()
        {
            // arrange
            string primary = CaReferenceString;
            string secondary = null;

            // act
            var attestation = X509Attestation.CreateFromCaReferences(primary, secondary);

            // assert
            Assert.IsNotNull(attestation.CaReferences.Primary);
            Assert.IsNull(attestation.CaReferences.Secondary);
            Assert.IsNull(attestation.ClientCertificates);
            Assert.IsNull(attestation.RootCertificates);
        }
    }
}
