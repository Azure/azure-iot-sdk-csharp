// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SystemTextJsonPayloadConventionTests
    {
        private static readonly SystemTextJsonPayloadConvention s_cut = SystemTextJsonPayloadConvention.Instance;

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsInt()
        {
            // arrange
            const int expected = 1;

            // act
            s_cut.GetObject<int>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsBool()
        {
            // arrange
            bool expected = true;

            // act
            s_cut.GetObject<bool>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsDateTimeOffset()
        {
            // arrange
            DateTimeOffset expected = DateTimeOffset.UtcNow;

            // act
            s_cut.GetObject<DateTimeOffset>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsDateTime()
        {
            // arrange
            DateTime expected = DateTime.UtcNow;

            // act
            s_cut.GetObject<DateTime>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsString()
        {
            // arrange
            const string expected = nameof(SystemTextJsonPayloadConvention_RoundtripsString);

            // act
            s_cut.GetObject<string>(s_cut.GetObjectBytes(expected))
                .Should()
                .Be(expected);
        }

        [TestMethod]
        public void SystemTextJsonPayloadConvention_RoundtripsCustomType()
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
            [JsonPropertyName("intProp")]
            public int IntProp { get; set; }

            [JsonPropertyName("stringProp")]
            public string StringProp{ get; set; }
        }
    }
}
