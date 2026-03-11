// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    public class DefaultPayloadConventionTests
    {
        private static readonly DefaultPayloadConvention s_cut = DefaultPayloadConvention.Instance;
        private static readonly string s_dateTimeString = "2023-01-31T10:37:08.4599400";
        private static readonly string s_serializedPayloadString = "{\"time\":\"2023-01-31T10:37:08.4599400\"}";

        [TestMethod]
        public void DefaultPayloadConvention_DateTime_SerializesProperly()
        {
            // arrange
            var testDateTime = new TestDateTime { DateTimeString = s_dateTimeString };

            // act
            string result = DefaultPayloadConvention.Serialize(testDateTime);

            // assert
            result.Should().Be(s_serializedPayloadString);
        }

        [TestMethod]
        public void DefaultPayloadConvention_DateTime_DeserializesProperly()
        {
            // arrange
            string jsonStr = $@"{{""time"":""{s_dateTimeString}""}}";

            // act
            JObject payload = s_cut.GetObject<JObject>(jsonStr);

            //assert
            payload.ToString(Formatting.None).Should().Be(s_serializedPayloadString);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsInt()
        {
            // arrange
            const int expected = 1;

            // act
            s_cut.GetObject<int>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsBool()
        {
            // arrange
            bool expected = true;

            // act
            s_cut.GetObject<bool>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsDateTimeOffset()
        {
            // arrange
            DateTimeOffset expected = DateTimeOffset.UtcNow;

            // act
            s_cut.GetObject<DateTimeOffset>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsDateTime()
        {
            // arrange
            DateTime expected = DateTime.UtcNow;

            // act
            s_cut.GetObject<DateTime>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsString()
        {
            // arrange
            const string expected = nameof(DefaultPayloadConvention_RoundtripsString);

            // act
            s_cut.GetObject<string>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void DefaultPayloadConvention_RoundtripsCustomType()
        {
            // arrange
            var expected = new CustomType
            {
                IntProp = 1,
                StringProp = Guid.NewGuid().ToString(),
            };

            // act
            s_cut.GetObject<CustomType>(s_cut.GetObjectBytes(expected))
                .Should()
                .BeEquivalentTo(expected);
        }

        private class CustomType
        {
            [JsonProperty("intProp")]
            public int IntProp { get; set; }

            [JsonProperty("stringProp")]
            public string StringProp { get; set; }
        }

        private class TestDateTime
        {
            [JsonProperty("time")]
            public string DateTimeString { get; set; }
        }
    }
}
