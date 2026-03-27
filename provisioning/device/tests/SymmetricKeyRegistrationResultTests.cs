// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SymmetricKeyRegistrationResultTests
    {
        private const string FakeEnrollmentGroupId = "testing-enrollment-group-id";

        [TestMethod]
        public void SymmetricKeyRegistrationResult_EnrollmentGroupId()
        {
            // arrange

            var source = new SymmetricKeyRegistrationResult
            {
                EnrollmentGroupId = FakeEnrollmentGroupId,
            };
            string body = JsonSerializer.Serialize(source);

            // act
            SymmetricKeyRegistrationResult result = JsonSerializer.Deserialize<SymmetricKeyRegistrationResult>(body);

            // assert
            result.EnrollmentGroupId.Should().Be(source.EnrollmentGroupId);
        }
    }
}
