// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("UnitTest")]
    public class JsonSerializerSettingsInitializerTests
    {
        private static readonly string s_dateTimeString = "2023-01-31T10:37:08.4599400";
        private static readonly string s_serializedPayloadString = "{\"Iso8601String\":\"2023-01-31T10:37:08.4599400\"}";

        [TestMethod]
        public void JsonSerializerSettingsInitializer_SerializesProperly()
        {
            // arrange
            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetJsonSerializerSettingsDelegate();
            var testDateTime = new TestDateTime { Iso8601String = s_dateTimeString };

            // act
            var result = JsonConvert.SerializeObject(testDateTime);

            // assert
            result.Should().Be(s_serializedPayloadString);
        }

        [TestMethod]
        public void JsonSerializerSettingsInitializer_DeserializesProperly()
        {
            // arrange
            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetJsonSerializerSettingsDelegate();
            string jsonStr = $@"{{""Iso8601String"":""{s_dateTimeString}""}}";

            // act
            JObject payload = JsonConvert.DeserializeObject<JObject>(jsonStr);

            //assert
            payload.ToString(Formatting.None).Should().Be(s_serializedPayloadString);
        }

        private class TestDateTime
        {
            public string Iso8601String { get; set; }
        }
    }
}
