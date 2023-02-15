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
        private const string SubjectName = "testing-subject-name";
        private const string Sha1Thumbprint = "testing-sha1-thumbprint";
        private const string Sha256Thumbprint = "testing-sha256-thumbprint";
        private const string IssuerName = "testing-issuer-name";
        private const string SerialNumber = "testing-serial-number";
        private const int Version = 1;

        private static readonly DateTimeOffset s_notBeforeUtc = DateTimeOffset.MinValue;
        private static readonly DateTimeOffset s_notAfterUtc = DateTimeOffset.MaxValue;

        [TestMethod]
        public void X509CertificateInfo_Properties()
        {
            // arrange
            var source = new X509CertificateInfo
            {
                SubjectName= SubjectName,
                Sha1Thumbprint= Sha1Thumbprint,
                Sha256Thumbprint = Sha256Thumbprint,
                IssuerName= IssuerName,
                NotBeforeUtc= s_notBeforeUtc,
                NotAfterUtc= s_notAfterUtc,
                SerialNumber= SerialNumber,
                Version= Version,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509CertificateInfo certificateInfo = JsonConvert.DeserializeObject<X509CertificateInfo>(body);

            // assert

            certificateInfo.SubjectName.Should().Be(SubjectName);
            certificateInfo.Sha1Thumbprint.Should().Be(Sha1Thumbprint);
            certificateInfo.Sha256Thumbprint.Should().Be(Sha256Thumbprint);
            certificateInfo.IssuerName.Should().Be(IssuerName);
            certificateInfo.NotBeforeUtc.Should().Be(s_notBeforeUtc);
            certificateInfo.NotAfterUtc.Should().Be(s_notAfterUtc);
            certificateInfo.SerialNumber.Should().Be(SerialNumber);
            certificateInfo.Version.Should().Be(Version);
        }
    }
}
