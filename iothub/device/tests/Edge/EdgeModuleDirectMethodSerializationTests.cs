// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
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
            string edgeDirectMethodJson = JsonSerializer.Serialize(edgeDirectMethod, JsonSerializerSettings.Options);
            EdgeModuleDirectMethodRequest deserializedEdgeDirectMethod = JsonSerializer.Deserialize<EdgeModuleDirectMethodRequest>(edgeDirectMethodJson, JsonSerializerSettings.Options);
            // assert
            deserializedEdgeDirectMethod.Should().BeEquivalentTo(edgeDirectMethod);
            deserializedEdgeDirectMethod.Payload.Should().BeEquivalentTo(payload);
        }
    }
}
