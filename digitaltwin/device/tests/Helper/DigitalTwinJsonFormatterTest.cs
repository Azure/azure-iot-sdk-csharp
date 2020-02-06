// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Helper
{
    [Trait("TestCategory", "DigitalTwin")]
    [Trait("TestCategory", "Unit")]
    public class DigitalTwinJsonFormatterTest
    {
        [Fact]
        public void TestToObjectWhenGivenNull()
        {
            TestDigitalTwinJson obj = new DigitalTwinJsonFormatter().ToObject<TestDigitalTwinJson>(null);
            Assert.Equal(new TestDigitalTwinJson { Value1 = 0, Value2 = null }, obj);
        }

        [Fact]
        public void TestToObjectWhenGivenWhiteSpace()
        {
            TestDigitalTwinJson obj = new DigitalTwinJsonFormatter().ToObject<TestDigitalTwinJson>(" ");
            Assert.Equal(new TestDigitalTwinJson { Value1 = 0, Value2 = null }, obj);
        }

        [Fact]
        public void TestToObjectWhenGivenJObject()
        {
            var jobj2 = new DigitalTwinJsonFormatter().ToObject<object>("{\"Value1\":123,\"Value2\":\"OneTwoThree\"}");
            Assert.Equal("Microsoft.Azure.Devices.DigitalTwin.Client.Model.DataCollection", jobj2.GetType().FullName);
        }

        private struct TestDigitalTwinJson : IEquatable<TestDigitalTwinJson>
        {
            public int Value1 { get; set; }

            public string Value2 { get; set; }

            public bool Equals(TestDigitalTwinJson other)
            {
                return this.Value1.Equals(other.Value1) &&
                    string.Equals(this.Value2, other.Value2, StringComparison.Ordinal);
            }
        }
    }
}
