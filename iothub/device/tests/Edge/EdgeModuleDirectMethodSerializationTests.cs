// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests.Edge
{
    [TestClass]
    [TestCategory("Unit")]
    public class EdgeModuleDirectMethodSerializationTests
    {
        [TestMethod]
        public void EdgeModuleDirectMethodSerialization_SerializesCorrectly()
        {
            // arrange
            byte[] payload = Encoding.Unicode.GetBytes("testPayload");
            var edgeDirectMethod = new EdgeModuleDirectMethodRequest("testMethod", payload);

            // act
            string edgeDirectMethodJson = JsonSerializer.Serialize(edgeDirectMethod);  // todo this string contains the payload property with a value of a base64 encoded string? It seems like newtonsoft does that for us?
            EdgeModuleDirectMethodRequest deserializedEdgeDirectMethod = JsonSerializer.Deserialize<EdgeModuleDirectMethodRequest>(edgeDirectMethodJson);
            // assert
            deserializedEdgeDirectMethod.Should().BeEquivalentTo(edgeDirectMethod);
            deserializedEdgeDirectMethod.Payload.Should().BeEquivalentTo(payload);
        }
    }
}
