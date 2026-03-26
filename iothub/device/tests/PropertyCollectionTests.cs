// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class PropertyCollectionTests
    {
        public class TestObject
        {
            [JsonPropertyName("someProperty")]
            public String SomeProperty { get; set; }
        }

        [TestMethod]
        public void TestPropertyCollectionTypeCastingAndDeserialization()
        {
            Dictionary<string, object> props = new()
            {
                ["testDate"] = DateTimeOffset.MinValue,
                ["testString"] = "someString",
                ["testInt"] = int.MaxValue,
                ["testLong"] = long.MaxValue,
                ["testDouble"] = 1.23,
                ["testBoolean"] = false,
                ["testObject"] = new TestObject() { SomeProperty = "SomeComplexTypeValue" }
            };

            PropertyCollection collection = new(props, false);

            var expectedJson =
                "{" +
                "   \"testDate\":\"0001-01-01T00:00:00+00:00\"," +
                "   \"testString\":\"someString\"," +
                "   \"testInt\":2147483647," +
                "   \"testLong\":9223372036854775807," +
                "   \"testDouble\":1.23," +
                "   \"testBoolean\":false," +
                "   \"testObject\":" +
                "   {" +
                "       \"someProperty\":\"SomeComplexTypeValue\"" +
                "   }" +
                "}";

            Assert.IsInstanceOfType<DateTimeOffset>(collection["testDate"]);
            Assert.IsInstanceOfType<string>(collection["testString"]);
            Assert.IsInstanceOfType<int>(collection["testInt"]);
            Assert.IsInstanceOfType<long>(collection["testLong"]);
            Assert.IsInstanceOfType<double>(collection["testDouble"]);
            Assert.IsInstanceOfType<bool>(collection["testBoolean"]);
            Assert.IsInstanceOfType<JsonDictionary>(collection["testObject"]);
            Assert.IsInstanceOfType<string>(collection["testObject"]["someProperty"]);

            Assert.IsTrue(collection.TryGetValue("testDate", out DateTimeOffset testDate));
            Assert.IsTrue(collection.TryGetValue("testString", out string testString));
            Assert.IsTrue(collection.TryGetValue("testInt", out int testInt));
            Assert.IsTrue(collection.TryGetValue("testLong", out long testLong));
            Assert.IsTrue(collection.TryGetValue("testDouble", out double testDouble));
            Assert.IsTrue(collection.TryGetValue("testBoolean", out bool testBoolean));
            Assert.IsTrue(collection.TryGetAndDeserializeValue("testObject", out TestObject testObject));

            Assert.AreEqual(testDate, DateTimeOffset.MinValue);
            Assert.AreEqual(testString, "someString");
            Assert.AreEqual(testInt, 2147483647);
            Assert.AreEqual(testLong, 9223372036854775807);
            Assert.AreEqual(testDouble, 1.23);
            Assert.AreEqual(testBoolean, false);
            Assert.AreEqual(testObject.SomeProperty, "SomeComplexTypeValue");


            var actualJson = JsonSerializer.Serialize(collection, JsonSerializerSettings.Options);
            TestAssert.AreEqualJson(expectedJson, actualJson);
        }
    }
}
