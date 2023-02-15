// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Tests
{
    /// <summary>
    /// Ensures DateTime de/serializes properly using Newtonsoft.Json, avoiding a known bug: https://github.com/JamesNK/Newtonsoft.Json/issues/1511
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class NewtonsoftJsonPayloadSerializerTests
    {
        private static readonly string s_dateTimeString = "2023-01-31T10:37:08.4599400";
        private static readonly string s_serializedPayloadString = "{\"time\":\"2023-01-31T10:37:08.4599400\"}";

        [TestMethod]
        public void NewtonsoftJsonPayloadSerializer_DateTime_SerializesProperly()
        {
            // arrange
            var testDateTime = new TestDateTime { DateTimeString = s_dateTimeString };

            // act
            var result = NewtonsoftJsonPayloadSerializer.Instance.SerializeToString(testDateTime);

            // assert
            result.Should().Be(s_serializedPayloadString);
        }

        [TestMethod]
        public void NewtonsoftJsonPayloadSerializer_DateTime_DeserializesProperly()
        {
            // arrange
            string jsonStr = $@"{{""time"":""{s_dateTimeString}""}}";

            // act
            JObject payload = NewtonsoftJsonPayloadSerializer.Instance.DeserializeToType<JObject>(jsonStr);

            //assert
            payload.ToString(Formatting.None).Should().Be(s_serializedPayloadString);
        }

        private class TestDateTime
        {
            [JsonProperty("time")]
            public string DateTimeString { get; set; }
        }
    }
}
