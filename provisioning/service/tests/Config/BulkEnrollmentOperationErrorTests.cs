// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationErrorTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const int SampleErrorCode = 400;
        private const string SampleErrorStatus = "Bad message format exception.";

        /* SRS_BULK_ENROLLMENT_OPERATION_ERRO_21_002: [The BulkEnrollmentOperationError shall store the provided information.] */
        [TestMethod]
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
            BulkEnrollmentOperationError bulkError = JsonSerializer.Deserialize<BulkEnrollmentOperationError>(erroJson);

            // assert
            Assert.IsNotNull(bulkError);
            Assert.AreEqual(SampleRegistrationId, bulkError.RegistrationId);
            Assert.AreEqual(SampleErrorCode, bulkError.ErrorCode);
            Assert.AreEqual(SampleErrorStatus, bulkError.ErrorStatus);
        }

        [TestMethod]
        public void BulkEnrollmentOperationErrorConstructorSucceedOnOnlyRegistrationId()
        {
            // arrange
            string erroJson =
                "{ " +
                $"  \"registrationId\": \"{SampleRegistrationId}\"" +
                "}";

            // act
            BulkEnrollmentOperationError bulkError = JsonSerializer.Deserialize<BulkEnrollmentOperationError>(erroJson);

            // assert
            Assert.IsNotNull(bulkError);
            Assert.AreEqual(SampleRegistrationId, bulkError.RegistrationId);
            Assert.IsNull(bulkError.ErrorCode);
            Assert.IsNull(bulkError.ErrorStatus);
        }
    }
}
