// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class BulkEnrollmentOperationTests
    {
        private static readonly string s_primaryKey = CryptoKeyGenerator.GenerateKey(32);
        private static readonly string s_secondaryKey = CryptoKeyGenerator.GenerateKey(32);
        private static readonly IndividualEnrollment s_individualEnrollment1 = new("regid1", new SymmetricKeyAttestation(s_primaryKey, s_secondaryKey));
        private static readonly IndividualEnrollment s_individualEnrollment2 = new("regid2", new SymmetricKeyAttestation(s_primaryKey, s_secondaryKey));
        private static readonly List<IndividualEnrollment> s_individualEnrollments = new() { s_individualEnrollment1, s_individualEnrollment2 };

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
                "        \"attestation\": {" +
                "          \"type\": \"symmetricKey\"," +
                "          \"symmetricKey\": {\n" +
                $"           \"primaryKey\": \"{s_primaryKey}\",\n" +
                $"           \"secondaryKey\": \"{s_secondaryKey}\"\n" +
                "          }\n" +
                "        }\n" +
                "      }," +
                "      {\n" +
                "        \"registrationId\": \"regid2\",\n" +
                "        \"attestation\": {" +
                "          \"type\": \"symmetricKey\"," +
                "          \"symmetricKey\": {\n" +
                $"           \"primaryKey\": \"{s_primaryKey}\",\n" +
                $"           \"secondaryKey\": \"{s_secondaryKey}\"\n" +
                "          }\n" +
                "        }\n" +
                "      }" +
                "    ]" +
                "}";

            // act
            IndividualEnrollmentBulkOperation operation = new IndividualEnrollmentBulkOperation()
            {
                Mode = BulkOperationMode.Create,
                Enrollments = s_individualEnrollments,
            };

            string bulkJson = JsonConvert.SerializeObject(operation);

            // assert
            TestAssert.AreEqualJson(expectedJson, bulkJson);
        }
    }
}
