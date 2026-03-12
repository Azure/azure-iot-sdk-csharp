// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationErrorTests
    {
        private const string SampleRegistrationId = "valid-registration-id";
        private const int SampleErrorCode = 400;
        private const string SampleErrorStatus = "Bad message format exception.";

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
            BulkEnrollmentOperationError bulkError = JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(erroJson);

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
            BulkEnrollmentOperationError bulkError = JsonConvert.DeserializeObject<BulkEnrollmentOperationError>(erroJson);

            // assert
            Assert.IsNotNull(bulkError);
            Assert.AreEqual(SampleRegistrationId, bulkError.RegistrationId);
            Assert.IsNull(bulkError.ErrorCode);
            Assert.IsNull(bulkError.ErrorStatus);
        }
    }
}
