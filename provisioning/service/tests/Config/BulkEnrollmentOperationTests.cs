// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationTests
    {
        private static IndividualEnrollment individualEnrollment1 = new IndividualEnrollment("regid1", new TpmAttestation("abc="));
        private static IndividualEnrollment individualEnrollment2 = new IndividualEnrollment("regid2", new TpmAttestation("abc="));
        private static IEnumerable<IndividualEnrollment> individualEnrollments = new List<IndividualEnrollment>() { individualEnrollment1, individualEnrollment2 };


        /* SRS_BULK_OPERATION_21_001: [The toJsonElement shall throw ArgumentException if the provided collection of 
                                        individualEnrollments is null or empty.] */
        [TestMethod]
        public void BulkEnrollmentOperationToJsonThrowsOnInvalidParameters()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, null));
            TestAssert.Throws<ArgumentException>(() => BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, new List<IndividualEnrollment>()));
        }

        /* SRS_BULK_OPERATION_21_002: [The toJson shall return a String with the mode and the collection of individualEnrollments 
                                        using a JSON format.] */
        [TestMethod]
        public void BulkEnrollmentOperationConstructorSucceed()
        {
            // arrange
            string expectedJson =
                "{" +
                "    \"mode\":\"create\"," +
                "    \"enrollments\": [ " +
                "      {\n" +
                "        \"registrationId\": \"regid1\",\n" +
                "        \"attestation\": {\n" +
                "          \"type\": \"tpm\",\n" +
                "          \"tpm\": {\n" +
                "            \"endorsementKey\": \"abc=\"\n" +
                "          }\n" +
                "        }\n" +
                "      }," +
                "      {\n" +
                "        \"registrationId\": \"regid2\",\n" +
                "        \"attestation\": {\n" +
                "          \"type\": \"tpm\",\n" +
                "          \"tpm\": {\n" +
                "            \"endorsementKey\": \"abc=\"\n" +
                "          }\n" +
                "        }\n" +
                "      }" +
                "    ]" +
                "}";

            // act
            string bulkJson = BulkEnrollmentOperation.ToJson(BulkOperationMode.Create, individualEnrollments);

            // assert
            TestAssert.AreEqualJson(expectedJson, bulkJson);
        }
    }
}
