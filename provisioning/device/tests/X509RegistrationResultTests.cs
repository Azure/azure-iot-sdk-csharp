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
        private static readonly string s_enrollmentGroupId = "testing-enrollment-group-id";
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
            result.CertificateInfo.Should().BeEquivalentTo(s_certificateInfo);
        }

        [TestMethod]
        public void X509RegistrationResult_EnrollmentGroupId()
        {
            // arrange

            var source = new X509RegistrationResult
            {
                EnrollmentGroupId = s_enrollmentGroupId,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            X509RegistrationResult result = JsonConvert.DeserializeObject<X509RegistrationResult>(body);

            // assert
            result.EnrollmentGroupId.Should().Be(s_enrollmentGroupId);
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
            result.SigningCertificateInfo.Should().BeEquivalentTo(s_certificateInfo);
        }
    }
}
