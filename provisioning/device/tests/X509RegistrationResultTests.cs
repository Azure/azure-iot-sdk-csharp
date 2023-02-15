// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class X509RegistrationResultTests
    {
        private const string FakeEnrollmentGroupId = "testing-enrollment-group-id";
        private static readonly X509CertificateInfo s_certificateInfo = new();

        [TestMethod]
        public void X509RegistrationResult_CertificateInfo()
        {
            // arrange

            var source = new X509RegistrationResult
            {
                CertificateInfo = s_certificateInfo,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509RegistrationResult result = JsonConvert.DeserializeObject<X509RegistrationResult>(body);

            // assert
            result.CertificateInfo.Should().BeEquivalentTo(source.CertificateInfo);
        }

        [TestMethod]
        public void X509RegistrationResult_EnrollmentGroupId()
        {
            // arrange

            var source = new X509RegistrationResult
            {
                EnrollmentGroupId = FakeEnrollmentGroupId,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509RegistrationResult result = JsonConvert.DeserializeObject<X509RegistrationResult>(body);

            // assert
            result.EnrollmentGroupId.Should().Be(source.EnrollmentGroupId);
        }

        [TestMethod]
        public void X509RegistrationResult_SigningCertificateInfo()
        {
            // arrange

            var source = new X509RegistrationResult
            {
                SigningCertificateInfo = s_certificateInfo,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509RegistrationResult result = JsonConvert.DeserializeObject<X509RegistrationResult>(body);

            // assert
            result.SigningCertificateInfo.Should().BeEquivalentTo(source.SigningCertificateInfo);
        }
    }
}
