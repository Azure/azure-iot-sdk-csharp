// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class DirectMethodResponseSerializationTests
    {
        [TestMethod]
        public void DirectMethodResponseSerializationTest_ResponseSerializesCorrectly()
        {
            // arrange
            var directMethodResponse = new DirectMethodResponse(200)
            {
                Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize("testPayload"))
            };

            // act
            string serializedDirectMethodResponse = JsonSerializer.Serialize(directMethodResponse);
            DirectMethodResponse deserializedResponse = JsonSerializer.Deserialize<DirectMethodResponse>(serializedDirectMethodResponse);

            // assert
            deserializedResponse.Should().BeEquivalentTo(directMethodResponse);
            deserializedResponse.Payload.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(JsonSerializer.Serialize("testPayload")));
        }
    }
}
