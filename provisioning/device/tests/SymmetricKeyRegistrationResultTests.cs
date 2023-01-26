// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SymmetricKeyRegistrationResultTests
    {
        private static readonly string s_enrollmentGroupId = "testing-enrollment-group-id";

        [TestMethod]
        public void SymmetricKeyRegistrationResult_EnrollmentGroupId()
        {
            // arrange

            var source = new SymmetricKeyRegistrationResult
            {
                EnrollmentGroupId = s_enrollmentGroupId,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            SymmetricKeyRegistrationResult result = JsonConvert.DeserializeObject<SymmetricKeyRegistrationResult>(body);

            // assert
            result.EnrollmentGroupId.Should().Be(s_enrollmentGroupId);
        }
    }
}
