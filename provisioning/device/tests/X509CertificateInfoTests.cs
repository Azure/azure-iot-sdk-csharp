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
        private const string FakeSubjectName = "testing-subject-name";
        private const string FakeSha1Thumbprint = "testing-sha1-thumbprint";
        private const string FakeSha256Thumbprint = "testing-sha256-thumbprint";
        private const string FakeIssuerName = "testing-issuer-name";
        private const string FakeSerialNumber = "testing-serial-number";
        private const int FakeVersion = 1;

        private static readonly DateTimeOffset s_notBeforeUtc = DateTimeOffset.MinValue;
        private static readonly DateTimeOffset s_notAfterUtc = DateTimeOffset.MaxValue;

        [TestMethod]
        public void X509CertificateInfo_Properties()
        {
            // arrange
            var source = new X509CertificateInfo
            {
                SubjectName= FakeSubjectName,
                Sha1Thumbprint= FakeSha1Thumbprint,
                Sha256Thumbprint = FakeSha256Thumbprint,
                IssuerName= FakeIssuerName,
                NotBeforeUtc= s_notBeforeUtc,
                NotAfterUtc= s_notAfterUtc,
                SerialNumber= FakeSerialNumber,
                Version= FakeVersion,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509CertificateInfo certificateInfo = JsonConvert.DeserializeObject<X509CertificateInfo>(body);

            // assert

            certificateInfo.SubjectName.Should().Be(source.SubjectName);
            certificateInfo.Sha1Thumbprint.Should().Be(source.Sha1Thumbprint);
            certificateInfo.Sha256Thumbprint.Should().Be(source.Sha256Thumbprint);
            certificateInfo.IssuerName.Should().Be(source.IssuerName);
            certificateInfo.NotBeforeUtc.Should().Be(source.NotBeforeUtc);
            certificateInfo.NotAfterUtc.Should().Be(source.NotAfterUtc);
            certificateInfo.SerialNumber.Should().Be(source.SerialNumber);
            certificateInfo.Version.Should().Be(source.Version);
        }
    }
}
