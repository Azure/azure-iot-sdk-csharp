// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("testPayload"))
            };

            // act
            string serializedDirectMethodResponse = JsonConvert.SerializeObject(directMethodResponse);
            DirectMethodResponse deserializedResponse = JsonConvert.DeserializeObject<DirectMethodResponse>(serializedDirectMethodResponse);

            // assert
            deserializedResponse.Should().BeEquivalentTo(directMethodResponse);
            deserializedResponse.Payload.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("testPayload")));
        }
    }
}
