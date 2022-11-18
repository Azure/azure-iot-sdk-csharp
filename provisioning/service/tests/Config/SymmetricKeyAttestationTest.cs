// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class SymmetricKeyAttestationTest
    {
        private readonly string _validKeyValue = Convert.ToBase64String(Encoding.UTF8.GetBytes("000000000000000000"));
        private readonly string _validKeyValue2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("111111111111111111"));

        [TestMethod]
        public void ConstructorAllowsBase64EncodedKeys()
        {
            var symmetricKeyAttestation = new SymmetricKeyAttestation(_validKeyValue, _validKeyValue2);

            symmetricKeyAttestation.PrimaryKey.Should().Be(_validKeyValue);
            symmetricKeyAttestation.SecondaryKey.Should().Be(_validKeyValue2);
        }

        [TestMethod]
        public void JsonSerializingWorks()
        {
            string expectedJson =
                "{" +
                "\"primaryKey\":\"" + _validKeyValue + "\"," +
                "\"secondaryKey\":\"" + _validKeyValue2 + "\"" +
                "}";

            var symmetricKeyAttestation = new SymmetricKeyAttestation(_validKeyValue, _validKeyValue2);

            string json = JsonSerializer.Serialize(symmetricKeyAttestation);

            json.Should().Be(expectedJson);
        }

        [TestMethod]
        public void JsonParsingWorks()
        {
            string expectedJson =
                "{" +
                "  \"primaryKey\":\"" + _validKeyValue + "\"," +
                "  \"secondaryKey\":\"" + _validKeyValue2 + "\"" +
                "}";

            var symmetricKeyAttestation = JsonSerializer.Deserialize<SymmetricKeyAttestation>(expectedJson);

            symmetricKeyAttestation.PrimaryKey.Should().Be(_validKeyValue);
            symmetricKeyAttestation.SecondaryKey.Should().Be(_validKeyValue2);
        }
    }
}
