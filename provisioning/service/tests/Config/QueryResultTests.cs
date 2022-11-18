// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class QueryResultTests
    {
        private const string SerializedNameUnknown = "unknown";
        private const string SerializedNameEnrollment = "enrollment";
        private const string SerializedNameEnrollmentGroup = "enrollmentGroup";
        private const string SerializedNameDeviceRegistration = "deviceRegistration";
        private const string SampleContinuationToken = "{\"token\":\"+RID:Defghij6KLMNOPQ==#RS:1#TRC:2#FPC:AUAAAAAAAAAJQABAAAAAAAk=\",\"range\":{\"min\":\"0123456789abcd\",\"max\":\"FF\"}}";
        private const string SampleListIntJSON = "[1, 2, 3]";
        private const string SampleListJObjectJSON = "[{\"a\":1}, {\"a\":2}, {\"a\":3}]";

        private const string SampleEnrollmentJson1 =
                "    {\n" +
                "      \"registrationId\": \"registrationid-ae518a62-3480-4639-bce2-5b69a3bb35a3\",\n" +
                "      \"deviceId\": \"JavaDevice-c743c684-2190-4062-a5a8-efc416ad4dba\",\n" +
                "      \"attestation\": {\n" +
                "        \"type\": \"tpm\",\n" +
                "        \"tpm\": {\n" +
                "          \"endorsementKey\": \"randonendorsementkeyfortest==\"\n" +
                "        }\n" +
                "      },\n" +
                "      \"iotHubHostName\": \"ContosoIotHub.azure-devices.net\",\n" +
                "      \"provisioningStatus\": \"enabled\",\n" +
                "      \"createdDateTimeUtc\": \"2017-09-19T15:45:53.3981876Z\",\n" +
                "      \"lastUpdatedDateTimeUtc\": \"2017-09-19T15:45:53.3981876Z\",\n" +
                "      \"etag\": \"00000000-0000-0000-0000-00000000000\"\n" +
                "    }";
        private const string SampleEnrollmentJson2 =
                "    {\n" +
                "      \"registrationId\": \"registrationid-6bdaeb7c-51fc-4a67-b24e-64e42d3aa698\",\n" +
                "      \"deviceId\": \"JavaDevice-eb17e87a-11aa-4794-944f-bbbf1fb960a0\",\n" +
                "      \"attestation\": {\n" +
                "        \"type\": \"tpm\",\n" +
                "        \"tpm\": {\n" +
                "          \"endorsementKey\": \"randonendorsementkeyfortest==\"\n" +
                "        }\n" +
                "      },\n" +
                "      \"iotHubHostName\": \"ContosoIotHub.azure-devices.net\",\n" +
                "      \"provisioningStatus\": \"enabled\",\n" +
                "      \"createdDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "      \"lastUpdatedDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "      \"etag\": \"00000000-0000-0000-0000-00000000000\"\n" +
                "    }";
        private const string SampleEnrollmentsJSON =
            "[\n" +
            SampleEnrollmentJson1 + ",\n" +
            SampleEnrollmentJson2 +
            "]";

        private const string SampleEnrollmentGroupId = "valid-enrollment-group-id";
        private const string SampleCreateDateTimeUTCString = "2017-11-14T12:34:18.123Z";
        private const string SampleLastUpdatedDateTimeUTCString = "2017-11-14T12:34:18.321Z";
        private const string SampleEtag = "00000000-0000-0000-0000-00000000000";
        private const string SampleEnrollmentGroupJson1 =
            "{\n" +
            "   \"enrollmentGroupId\":\"" + SampleEnrollmentGroupId + "\",\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"signingCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleEnrollmentGroupId + "\",\n" +
            "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                       \"serialNumber\": \"000000000000000000\",\n" +
            "                       \"version\": 3\n" +
            "                   }\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   },\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\",\n" +
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";
        private const string SampleEnrollmentGroupJson2 =
            "{\n" +
            "   \"enrollmentGroupId\":\"" + SampleEnrollmentGroupId + "\",\n" +
            "   \"attestation\":{\n" +
            "       \"type\":\"x509\",\n" +
            "       \"x509\":{\n" +
            "           \"signingCertificates\":{\n" +
            "               \"primary\":{\n" +
            "                   \"info\": {\n" +
            "                       \"subjectName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"sha1Thumbprint\": \"0000000000000000000000000000000000\",\n" +
            "                       \"sha256Thumbprint\": \"" + SampleEnrollmentGroupId + "\",\n" +
            "                       \"issuerName\": \"CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US\",\n" +
            "                       \"notBeforeUtc\": \"2017-11-14T12:34:18Z\",\n" +
            "                       \"notAfterUtc\": \"2017-11-20T12:34:18Z\",\n" +
            "                       \"serialNumber\": \"000000000000000000\",\n" +
            "                       \"version\": 3\n" +
            "                   }\n" +
            "               }\n" +
            "           }\n" +
            "       }\n" +
            "   },\n" +
            "   \"createdDateTimeUtc\": \"" + SampleCreateDateTimeUTCString + "\",\n" +
            "   \"lastUpdatedDateTimeUtc\": \"" + SampleLastUpdatedDateTimeUTCString + "\",\n" +
            "   \"etag\": \"" + SampleEtag + "\"\n" +
            "}";

        private const string SampleEnrollmentGroupJSON =
                "[\n" +
                SampleEnrollmentGroupJson1 + ",\n" +
                SampleEnrollmentGroupJson2 +
                "]";

        private const string SampleRegistrationStatus1 =
                "{\n" +
                "    \"registrationId\":\"registrationid-ae518a62-3480-4639-bce2-5b69a3bb35a3\",\n" +
                "    \"createdDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "    \"assignedHub\":\"ContosoIotHub.azure-devices.net\",\n" +
                "    \"deviceId\":\"JavaDevice-c743c684-2190-4062-a5a8-efc416ad4dba\",\n" +
                "    \"status\":\"assigned\",\n" +
                "    \"lastUpdatedDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "    \"errorCode\": 200,\n" +
                "    \"errorMessage\":\"Succeeded\",\n" +
                "    \"etag\": \"00000000-0000-0000-0000-00000000000\"\n" +
                "}";
        private const string SampleRegistrationStatus2 =
                "{\n" +
                "    \"registrationId\":\"registrationid-6bdaeb7c-51fc-4a67-b24e-64e42d3aa698\",\n" +
                "    \"createdDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "    \"assignedHub\":\"ContosoIotHub.azure-devices.net\",\n" +
                "    \"deviceId\":\"JavaDevice-c743c684-2190-4062-a5a8-efc416ad4dba\",\n" +
                "    \"status\":\"assigned\",\n" +
                "    \"lastUpdatedDateTimeUtc\": \"2017-09-19T15:46:35.1533673Z\",\n" +
                "    \"errorCode\": 200,\n" +
                "    \"errorMessage\":\"Succeeded\",\n" +
                "    \"etag\": \"00000000-0000-0000-0000-00000000000\"\n" +
                "}";
        private const string SampleRegistrationStatusJSON =
                "[\n" +
                SampleRegistrationStatus1 + ",\n" +
                SampleRegistrationStatus2 +
                "]";

        [TestMethod]
        public void QueryResultConstructorThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => new QueryResult(null, SampleListIntJSON, SampleContinuationToken));
            TestAssert.Throws<ArgumentException>(() => new QueryResult("", SampleListIntJSON, SampleContinuationToken));
            TestAssert.Throws<ArgumentException>(() => new QueryResult("InvalidType", SampleListIntJSON, SampleContinuationToken));
            TestAssert.Throws<ArgumentException>(() => new QueryResult(SerializedNameEnrollment, null, SampleContinuationToken));
            TestAssert.Throws<ArgumentException>(() => new QueryResult(SerializedNameEnrollment, "", SampleContinuationToken));
            TestAssert.Throws<JsonException>(() => new QueryResult(SerializedNameEnrollment, "[1, 2, ]", SampleContinuationToken));
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnIndividualEnrollment()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameEnrollment, SampleEnrollmentsJSON, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.Enrollment, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(2, items.Count());
            Assert.IsTrue(items.FirstOrDefault() is IndividualEnrollment);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnEnrollmentGroup()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameEnrollmentGroup, SampleEnrollmentGroupJSON, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.EnrollmentGroup, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(2, items.Count());
            Assert.IsTrue(items.FirstOrDefault() is EnrollmentGroup);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnDeviceRegistration()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameDeviceRegistration, SampleRegistrationStatusJSON, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.DeviceRegistration, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(2, items.Count());
            Assert.IsTrue(items.FirstOrDefault() is DeviceRegistrationState);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnUnknownWithNullBody()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameUnknown, null, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.Unknown, queryResult.QueryType);
            Assert.IsNull(queryResult.Items);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnUnknownWithObjectListBody()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameUnknown, SampleListJObjectJSON, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.Unknown, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(3, items.Count());
            Assert.IsTrue(items.FirstOrDefault() is JObject);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnUnknownWithIntegerListBody()
        {
            // arrange - act
            var queryResult = new QueryResult(SerializedNameUnknown, SampleListIntJSON, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.Unknown, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(3, items.Count());
            Assert.IsTrue(items.FirstOrDefault() is long);
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnUnknownWithStringBody()
        {
            // arrange
            string body = "This is a non deserializable body";

            // act
            var queryResult = new QueryResult(SerializedNameUnknown, body, SampleContinuationToken);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.AreEqual(QueryResultType.Unknown, queryResult.QueryType);
            IEnumerable<Object> items = queryResult.Items;
            Assert.AreEqual(1, items.Count());
            Assert.AreEqual(body, items.FirstOrDefault());
            Assert.AreEqual(SampleContinuationToken, queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnNullContinuationToken()
        {
            // arrange
            string body = "This is a non deserializable body";

            // act
            var queryResult = new QueryResult(SerializedNameUnknown, body, null);

            // assert
            Assert.IsNotNull(queryResult);
            Assert.IsNull(queryResult.ContinuationToken);
        }

        [TestMethod]
        public void QueryResultConstructorSucceedOnEmptyContinuationToken()
        {
            // arrange
            string body = "This is a non deserializable body";

            // act
            var queryResult = new QueryResult(SerializedNameUnknown, body, "");

            // assert
            Assert.IsNotNull(queryResult);
            Assert.IsNull(queryResult.ContinuationToken);
        }
    }
}
