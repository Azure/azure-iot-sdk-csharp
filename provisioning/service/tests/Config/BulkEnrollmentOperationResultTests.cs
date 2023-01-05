// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationResultTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const int SampleErrorCode = 400;
        private const string SampleErrorStatus = "Bad message format exception.";
        private static readonly string s_sampleValidErrorJson =
            "{ " +
            $"  \"registrationId\": \"{SampleRegistrationId}\", " +
            $"  \"errorCode\": {SampleErrorCode}, " +
            $"  \"errorStatus\": \"{SampleErrorStatus}\" " +
            "}";

        [TestMethod]
        public void BulkEnrollmentOperationResultConstructorThrowsOnInvalidParameters()
        {
            // arrange
            string nonRegistrationId =
                "{" +
                "  \"isSuccessful\": true, \"errors\": [" +
                "    {" +
                $"      \"errorCode\": {SampleErrorCode}" +
                "    }" +
                "  ]" +
                "}";

            string nonStatus =
                "{" +
                $"  \"errors\": [ {s_sampleValidErrorJson} ] " +
                "}";

            // act - assert
            Action act = () => JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(nonRegistrationId);
            act.Should().Throw<JsonSerializationException>();

            act = () => JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(nonStatus);
            act.Should().Throw<JsonSerializationException>();
        }

        [TestMethod]
        public void BulkEnrollmentOperationResultConstructorSucceed()
        {
            // arrange
            string validJson =
                "{" +
                $"  \"isSuccessful\": true, \"errors\": [ {s_sampleValidErrorJson}, {s_sampleValidErrorJson} ]" +
                "}";

            // act
            BulkEnrollmentOperationResult bulkErrors = JsonConvert.DeserializeObject<BulkEnrollmentOperationResult>(validJson);

            // assert
            Assert.IsNotNull(bulkErrors);
            Assert.IsTrue(bulkErrors.IsSuccessful);
            Assert.IsNotNull(bulkErrors.Errors);
            foreach (BulkEnrollmentOperationError item in bulkErrors.Errors)
            {
                Assert.AreEqual(SampleRegistrationId, item.RegistrationId);
            }
        }
    }
}
