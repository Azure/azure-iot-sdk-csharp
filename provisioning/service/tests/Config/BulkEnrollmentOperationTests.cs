// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
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
            string initialJson =
                "{" +
                "  \"mode\":\"create\"," +
                "    \"enrollments\": [ " +
                "    {\n" +
                "      \"attestation\": {" +
                "        \"type\": \"symmetricKey\"," +
                "        \"symmetricKey\": {\n" +
                $"          \"primaryKey\": \"{s_primaryKey}\",\n" +
                $"          \"secondaryKey\": \"{s_secondaryKey}\"\n" +
                "        }\n" +
                "      },\n" +
                "      \"registrationId\": \"regid1\"\n" +
                "    }," +
                "    {\n" +
                "      \"attestation\": {" +
                "        \"type\": \"symmetricKey\"," +
                "        \"symmetricKey\": {\n" +
                $"         \"primaryKey\": \"{s_primaryKey}\",\n" +
                $"         \"secondaryKey\": \"{s_secondaryKey}\"\n" +
                "        }\n" +
                "      },\n" +
                "      \"registrationId\": \"regid2\"\n" +
                "    }" +
                "  ]" +
                "}";
            var stripWhiteSpace = new Regex(@"\s", RegexOptions.Multiline);
            string expected = stripWhiteSpace.Replace(initialJson, "");

            // act
            var operation = new IndividualEnrollmentBulkOperation
            {
                Mode = BulkOperationMode.Create,
                Enrollments = s_individualEnrollments,
            };

            string actual = JsonConvert.SerializeObject(operation);

            // assert
            actual.Should().Be(expected);
        }
    }
}
