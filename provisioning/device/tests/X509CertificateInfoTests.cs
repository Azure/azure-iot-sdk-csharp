// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509CertificateInfoTests
    {
        private static readonly string s_subjectName = "testing-subject-name";
        private static readonly string s_sha1Thumbprint = "testing-sha1-thumbprint";
        private static readonly string s_sha256Thumbprint = "testing-sha256-thumbprint";
        private static readonly string s_issuerName = "testing-issuer-name";
        private static readonly DateTimeOffset s_notBeforeUtc = DateTimeOffset.MinValue;
        private static readonly DateTimeOffset s_notAfterUtc = DateTimeOffset.MaxValue;
        private static readonly string s_serialNumber = "testing-serial-number";
        private static readonly int s_version = 1;

        [TestMethod]
        public void X509CertificateInfo_Properties()
        {
            // arrange
            var source = new X509CertificateInfo
            {
                SubjectName= s_subjectName,
                Sha1Thumbprint= s_sha1Thumbprint,
                Sha256Thumbprint = s_sha256Thumbprint,
                IssuerName= s_issuerName,
                NotBeforeUtc= s_notBeforeUtc,
                NotAfterUtc= s_notAfterUtc,
                SerialNumber= s_serialNumber,
                Version= s_version,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509CertificateInfo certificateInfo = JsonConvert.DeserializeObject<X509CertificateInfo>(body);

            // assert

            certificateInfo.SubjectName.Should().Be(s_subjectName);
            certificateInfo.Sha1Thumbprint.Should().Be(s_sha1Thumbprint);
            certificateInfo.Sha256Thumbprint.Should().Be(s_sha256Thumbprint);
            certificateInfo.IssuerName.Should().Be(s_issuerName);
            certificateInfo.NotBeforeUtc.Should().Be(s_notBeforeUtc);
            certificateInfo.NotAfterUtc.Should().Be(s_notAfterUtc);
            certificateInfo.SerialNumber.Should().Be(s_serialNumber);
            certificateInfo.Version.Should().Be(s_version);
        }
    }
}
