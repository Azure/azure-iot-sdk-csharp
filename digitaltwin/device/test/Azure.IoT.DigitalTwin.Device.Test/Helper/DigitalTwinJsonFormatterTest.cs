// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Iot.DigitalTwin.Device.Helper;
using Newtonsoft.Json;
using Pose;
using Xunit;

namespace Azure.IoT.DigitalTwin.Device.Test.Helper
{
    public class DigitalTwinJsonFormatterTest
    {
        [Fact]
        public void TestFromObjectCallingToJsonLib()
        {
            bool jsonConvertStaticMethodCalled = false;
            Shim jsonConvertSerializeObjectShim = Shim.Replace(() => JsonConvert.SerializeObject(Is.A<object>())).With(
                delegate(object obj)
                {
                    jsonConvertStaticMethodCalled = true;
                    return JsonConvert.SerializeObject(obj);
                });

            PoseContext.Isolate(
                () =>
                {
                    var testObj = new TestDigitalTwinJson() { Value1 = 999, Value2 = "OneTwoThree" };
                    new DigitalTwinJsonFormatter().FromObject(testObj);

                    Assert.True(jsonConvertStaticMethodCalled);
                }, jsonConvertSerializeObjectShim);
        }

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
        public void TestToObjectCallingToJsonLib()
        {
            bool jsonConvertStaticMethodCalled = false;
            Shim jsonConvertDeserializeObjectShim = Shim.Replace(() => JsonConvert.DeserializeObject<TestDigitalTwinJson>(Is.A<string>())).With(
                delegate(string json)
                {
                    jsonConvertStaticMethodCalled = true;
                    return JsonConvert.DeserializeObject<TestDigitalTwinJson>(json);
                });

            PoseContext.Isolate(
                () =>
                {
                    new DigitalTwinJsonFormatter().ToObject<TestDigitalTwinJson>("{\"Value1\":999,\"Value2\":\"OneTwoThree\"}");

                    Assert.True(jsonConvertStaticMethodCalled);
                }, jsonConvertDeserializeObjectShim);
        }

        [Fact]
        public void TestToObjectWhenGivenJObject()
        {
            var jobj2 = new DigitalTwinJsonFormatter().ToObject<object>("{\"Value1\":123,\"Value2\":\"OneTwoThree\"}");
            Assert.Equal("Azure.Iot.DigitalTwin.Device.Model.DataCollection", jobj2.GetType().FullName);
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
