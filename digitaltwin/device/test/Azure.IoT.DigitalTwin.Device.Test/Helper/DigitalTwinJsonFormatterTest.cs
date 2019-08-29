// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Iot.DigitalTwin.Device.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Pose;

namespace Azure.IoT.DigitalTwin.Device.Test.Helper
{
    [TestClass]
    public class DigitalTwinJsonFormatterTest
    {
        [TestMethod]
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

                    Assert.IsTrue(jsonConvertStaticMethodCalled);
                }, jsonConvertSerializeObjectShim);
        }

        [TestMethod]
        public void TestToObjectWhenGivenNull()
        {
            TestDigitalTwinJson obj = new DigitalTwinJsonFormatter().ToObject<TestDigitalTwinJson>(null);
            Assert.AreEqual(new TestDigitalTwinJson { Value1 = 0, Value2 = null }, obj);
        }

        [TestMethod]
        public void TestToObjectWhenGivenWhiteSpace()
        {
            TestDigitalTwinJson obj = new DigitalTwinJsonFormatter().ToObject<TestDigitalTwinJson>(" ");
            Assert.AreEqual(new TestDigitalTwinJson { Value1 = 0, Value2 = null }, obj);
        }

        [TestMethod]
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

                    Assert.IsTrue(jsonConvertStaticMethodCalled);
                }, jsonConvertDeserializeObjectShim);
        }

        [TestMethod]
        public void TestToObjectWhenGivenJObject()
        {
            var jobj2 = new DigitalTwinJsonFormatter().ToObject<object>("{\"Value1\":123,\"Value2\":\"OneTwoThree\"}");
            Assert.AreEqual("Azure.Iot.DigitalTwin.Device.Model.DataCollection", jobj2.GetType().FullName);
        }

        private struct TestDigitalTwinJson : IEquatable<TestDigitalTwinJson>
        {
            public int Value1 { get; set; }

            public string Value2 { get; set; }

            public bool Equals(TestDigitalTwinJson other)
            {
                return this.Value1.Equals(other.Value1) &&
                    string.Equals(this.Value2, other.Value2);
            }
        }
    }
}
