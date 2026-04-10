// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class SymmetricKeyAttestationTest
    {
        string validKeyValue = Convert.ToBase64String(Encoding.UTF8.GetBytes("000000000000000000"));
        string validKeyValue2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("111111111111111111"));

        [TestMethod]
        public void constructorAllowsBase64EncodedKeys()
        {
            var symmetricKeyAttestation = new SymmetricKeyAttestation(validKeyValue, validKeyValue2);

            Assert.AreEqual(validKeyValue, symmetricKeyAttestation.PrimaryKey);
            Assert.AreEqual(validKeyValue2, symmetricKeyAttestation.SecondaryKey);
        }

        [TestMethod]
        public void jsonSerializingWorks()
        {
            string expectedJson =
                "{" +
                "\"primaryKey\":\"" + validKeyValue + "\"," +
                "\"secondaryKey\":\"" + validKeyValue2 + "\"" +
                "}";

            var symmetricKeyAttestation = new SymmetricKeyAttestation(validKeyValue, validKeyValue2);

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(symmetricKeyAttestation);

            Assert.AreEqual(expectedJson, json);
        }

        [TestMethod]
        public void jsonParsingWorks()
        {
            string expectedJson =
                "{" +
                "  \"primaryKey\":\"" + validKeyValue + "\"," +
                "  \"secondaryKey\":\"" + validKeyValue2 + "\"" +
                "}";

            SymmetricKeyAttestation symmetricKeyAttestation = Newtonsoft.Json.JsonConvert.DeserializeObject<SymmetricKeyAttestation>(expectedJson);

            Assert.AreEqual(validKeyValue, symmetricKeyAttestation.PrimaryKey);
            Assert.AreEqual(validKeyValue2, symmetricKeyAttestation.SecondaryKey);
        }
    }
}
