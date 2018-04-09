// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class BulkEnrollmentOperationErrorTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const int SampleErrorCode = 400;
        private const string SampleErrorStatus = "Bad message format exception.";

        /* SRS_BULK_ENROLLMENT_OPERATION_ERRO_21_001: [The BulkEnrollmentOperationError shall throws JsonSerializationException if the 
                                            provided registrationId is null, empty, or invalid.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void BulkEnrollmentOperationErrorConstructoThrowsOnInvalidParameters()
        {
            // arrange
            string nonRegistrationId = 
                "{" +
                $"  \"errorCode\": {SampleErrorCode}, \"errorStatus\": \"{SampleErrorStatus}\"" +
                "}";
            string emptyRegistrationId = 
                "{" +
                $"  \"registrationId\":\"\", \"errorCode\": {SampleErrorCode}, \"errorStatus\": \"{SampleErrorStatus}\"" +
                "}";
            string invalidRegistrationId = 
                "{" +
                $"  \"registrationId\":\"valid Registration Id\", \"errorCode\":{SampleErrorCode}, \"errorStatus\": \"{SampleErrorStatus}\"" +
                "}";

            // act - assert
            TestAssert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(nonRegistrationId));
            TestAssert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(emptyRegistrationId));
            TestAssert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(invalidRegistrationId));
        }

        /* SRS_BULK_ENROLLMENT_OPERATION_ERRO_21_002: [The BulkEnrollmentOperationError shall store the provided information.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void BulkEnrollmentOperationErrorConstructorSucceed()
        {
            // arrange
            string erroJson = 
                "{ " +
                $"  \"registrationId\": \"{SampleRegistrationId}\", " +
                $"  \"errorCode\": {SampleErrorCode}, " +
                $"  \"errorStatus\": \"{SampleErrorStatus}\"" +
                "}";

            // act
            BulkEnrollmentOperationError bulkError = JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(erroJson);

            // assert
            Assert.IsNotNull(bulkError);
            Assert.AreEqual(SampleRegistrationId, bulkError.RegistrationId);
            Assert.AreEqual(SampleErrorCode, bulkError.ErrorCode);
            Assert.AreEqual(SampleErrorStatus, bulkError.ErrorStatus);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void BulkEnrollmentOperationErrorConstructorSucceedOnOnlyRegistrationId()
        {
            // arrange
            string erroJson =
                "{ " +
                $"  \"registrationId\": \"{SampleRegistrationId}\"" +
                "}";

            // act
            BulkEnrollmentOperationError bulkError = JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(erroJson);

            // assert
            Assert.IsNotNull(bulkError);
            Assert.AreEqual(SampleRegistrationId, bulkError.RegistrationId);
            Assert.IsNull(bulkError.ErrorCode);
            Assert.IsNull(bulkError.ErrorStatus);
        }
    }
}
