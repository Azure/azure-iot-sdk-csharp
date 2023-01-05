// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using FluentAssertions;
using FluentAssertions.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificateInfoTests
    {
        private const string SubjectName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string Sha1Thumbprint = "0000000000000000000000000000000000";
        private const string Sha256Thumbprint = "validEnrollmentGroupId";
        private const string IssuerName = "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US";
        private const string NotBeforeUtcString = "2017-11-14T12:34:18.123Z";
        private readonly DateTime _notBeforeUtc = new(2017, 11, 14, 12, 34, 18, 123, DateTimeKind.Utc);
        private const string NOtAfterUtcString = "2017-11-14T12:34:18.321Z";
        private readonly DateTime _notAfterUtc = new(2017, 11, 14, 12, 34, 18, 321, DateTimeKind.Utc);
        private const string SerialNumber = "000000000000000000";
        private const int Version = 3;

        private readonly string[] _failDateTime = { null, string.Empty };

        private static string MakeJson(
            string subjectName, string sha1Thumbprint, string sha256Thumbprint,
            string issuerName, string notBeforeUtcString, string notAfterUtcString, string serialNumber, int version)
        {
            string json =
                "{" +
                (subjectName == null ? "" : "    \"subjectName\": \"" + subjectName + "\",") +
                (sha1Thumbprint == null ? "" : "    \"sha1Thumbprint\": \"" + sha1Thumbprint + "\",") +
                (sha256Thumbprint == null ? "" : "    \"sha256Thumbprint\": \"" + sha256Thumbprint + "\",") +
                (issuerName == null ? "" : "    \"issuerName\": \"" + issuerName + "\",") +
                (notBeforeUtcString == null ? "" : "    \"notBeforeUtc\": \"" + notBeforeUtcString + "\",") +
                (notAfterUtcString == null ? "" : "    \"notAfterUtc\": \"" + notAfterUtcString + "\",") +
                (serialNumber == null ? "" : "    \"serialNumber\": \"" + serialNumber + "\",") +
                "    \"version\": " + version +
                "}";

            return json;
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidNotBeforeUtc()
        {
            foreach (string failDateTime in _failDateTime)
            {
                // arrange
                string json = MakeJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, failDateTime, NOtAfterUtcString, SerialNumber, Version);

                // act - assert
                Action act = () => JsonConvert.DeserializeObject<X509CertificateInfo>(json);
                ExceptionAssertions<ProvisioningServiceException> error = act.Should().Throw<ProvisioningServiceException>();
                error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                error.And.IsTransient.Should().BeFalse();
            }
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidNotAfterUtc()
        {
            foreach (string failDateTime in _failDateTime)
            {
                // arrange
                string json = MakeJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, failDateTime, SerialNumber, Version);

                // act - assert
                Action act = () => JsonConvert.DeserializeObject<X509CertificateInfo>(json);
                ExceptionAssertions<ProvisioningServiceException> error = act.Should().Throw<ProvisioningServiceException>();
                error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                error.And.IsTransient.Should().BeFalse();
            }
        }

        [TestMethod]
        public void X509CertificateInfoThrowsOnInvalidVersion()
        {
            // arrange
            string json =
                "{" +
                "    \"subjectName\": \"" + SubjectName + "\"," +
                "    \"sha1Thumbprint\": \"" + Sha1Thumbprint + "\"," +
                "    \"sha256Thumbprint\": \"" + Sha256Thumbprint + "\"," +
                "    \"issuerName\": \"" + IssuerName + "\"," +
                "    \"notBeforeUtc\": \"" + NotBeforeUtcString + "\"," +
                "    \"notAfterUtc\": \"" + NOtAfterUtcString + "\"," +
                "    \"serialNumber\": \"" + SerialNumber + "\"" +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<X509CertificateInfo>(json);
            ExceptionAssertions<ProvisioningServiceException> error = act.Should().Throw<ProvisioningServiceException>();
            error.And.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.And.IsTransient.Should().BeFalse();
        }

        [TestMethod]
        public void X509CertificateInfoSucceedOnDeserialization()
        {
            // arrange
            string json = MakeJson(SubjectName, Sha1Thumbprint, Sha256Thumbprint, IssuerName, NotBeforeUtcString, NOtAfterUtcString, SerialNumber, Version);

            // act
            X509CertificateInfo x509CertificateInfo = JsonConvert.DeserializeObject<X509CertificateInfo>(json);

            // assert
            Assert.AreEqual(SubjectName, x509CertificateInfo.SubjectName);
            Assert.AreEqual(Sha1Thumbprint, x509CertificateInfo.Sha1Thumbprint);
            Assert.AreEqual(Sha256Thumbprint, x509CertificateInfo.Sha256Thumbprint);
            Assert.AreEqual(IssuerName, x509CertificateInfo.IssuerName);
            Assert.AreEqual(_notBeforeUtc, x509CertificateInfo.NotBeforeUtc);
            Assert.AreEqual(_notAfterUtc, x509CertificateInfo.NotAfterUtc);
            Assert.AreEqual(SerialNumber, x509CertificateInfo.SerialNumber);
            Assert.AreEqual(Version, x509CertificateInfo.Version);
        }
    }
}
