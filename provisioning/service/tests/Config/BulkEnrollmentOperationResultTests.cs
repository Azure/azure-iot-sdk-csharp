// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationResultTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const int SampleErrorCode = 400;
        private const string SampleErrorStatus = "Bad message format exception.";
        string SampleValidErrorJson =
            "{ " +
            $"  \"registrationId\": \"{SampleRegistrationId}\", " +
            $"  \"errorCode\": {SampleErrorCode}, " +
            $"  \"errorStatus\": \"{SampleErrorStatus}\" " +
            "}";

        /* SRS_BULK_ENROLLMENT_OPERATION_RESULT_21_001: [The BulkEnrollmentOperationResult shall throws JsonSerializationException if the 
                                            provided registrationId is null, empty, or invalid.] */
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
            string NonStatus = 
                "{" +
                $"  \"errors\": [ {SampleValidErrorJson} ] " +
                "}";

            // act - assert
            TestAssert.Throws<JsonException>(() => JsonSerializer.Deserialize<BulkEnrollmentOperationResult>(nonRegistrationId));
            TestAssert.Throws<JsonException>(() => JsonSerializer.Deserialize<BulkEnrollmentOperationResult>(NonStatus));
        }

        /* SRS_BULK_ENROLLMENT_OPERATION_RESULT_21_002: [The BulkEnrollmentOperationResult shall store the provided information.] */
        [TestMethod]
        public void BulkEnrollmentOperationResultConstructorSucceed()
        {
            // arrange
            string validJson = 
                "{" +
                $"  \"isSuccessful\": true, \"errors\": [ {SampleValidErrorJson}, {SampleValidErrorJson} ]" +
                "}";

            // act
            BulkEnrollmentOperationResult bulkErrors = JsonSerializer.Deserialize<BulkEnrollmentOperationResult>(validJson);

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
