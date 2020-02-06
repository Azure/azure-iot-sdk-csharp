// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Model
{
    [Trait("TestCategory", "DigitalTwin")]
    [Trait("TestCategory", "Unit")]
    public class DataCollectionTest
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            var dc = new DataCollection();

            Assert.Equal(new JObject().ToString(), dc.JObject.ToString());
        }

        [Fact]
        public void TestConstructorWithPropertiesJsonWhenGivenNull()
        {
            try
            {
                var dc = new DataCollection(null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal("propertiesJson", ex.ParamName);
                return;
            }

            Assert.True(false, "Expect exception thrown, but not happened.");
        }

        [Fact]
        public void TestConstructorWithPropertiesJsonWhenGivenWhiteSpace()
        {
            try
            {
                var dc = new DataCollection(" ");
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal("propertiesJson", ex.ParamName);
                return;
            }

            Assert.True(false, "Expect exception thrown, but not happened.");
        }

        [Fact]
        public void TestConstructorWithPropertiesJson()
        {
            string json = "{\"key1\": \"value1\", \"key2\": { \"value\": 123 } }";
            var dc = new DataCollection(json);

            Assert.Equal(JObject.Parse(json).ToString(), dc.JObject.ToString());
        }

        [Fact]
        public void TestToJson()
        {
            JObject jObj = new JObject();
            jObj.Add("Key1", 999);
            jObj.Add("Key2", "OneTwoThree");
            string json = JsonConvert.SerializeObject(jObj);

            var dataCollection = new DataCollection(json);
            string actualJson = dataCollection.ToJson();
            Assert.Equal(json, actualJson);
        }

        [Fact]
        public void TestGetEnumerator()
        {
            JObject jObj = new JObject();
            jObj.Add("Key1", 999);
            jObj.Add("Key2", "OneTwoThree");
            string json = JsonConvert.SerializeObject(jObj);

            int count = 0;
            var dataCollection = new DataCollection(json);

            foreach (KeyValuePair<string, object> item in dataCollection)
            {
                if (item.Key == "Key1")
                {
                    Assert.Equal(999, (long)((JValue)item.Value).Value);
                    count++;
                }
                else if (item.Key == "Key2")
                {
                    Assert.Equal("OneTwoThree", (string)((JValue)item.Value).Value);
                    count++;
                }
                else
                {
                    Assert.True(false, "data collection enumerate with unexpected key.");
                }
            }

            Assert.Equal(2, count);
        }

        [Fact]
        public void TestGetSetItem()
        {
            var dc = new DataCollection();
            dc["A"] = "{ \"key\": 123 }";

            var value = (JToken)dc["A"];

            Assert.NotNull(value);
            Assert.Equal("{ \"key\": 123 }", value);
        }

        [Fact]
        public void TestGetNonExistingItem()
        {
            var dc = new DataCollection();

            var value = (string)dc["A"];

            Assert.NotNull(value);
            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void TestSetExistingItem()
        {
            var dc = new DataCollection();
            dc["A"] = "{ \"key\": 123 }";
            dc["A"] = "{ \"key\": 456 }";

            var value = (JToken)dc["A"];

            Assert.NotNull(value);
            Assert.Equal("{ \"key\": 456 }", value);
        }

        [Fact]
        public void TestSetItemWithNullValue()
        {
            var dc = new DataCollection();
            dc["A"] = null;

            var value = (JToken)dc["A"];

            Assert.Equal(JToken.Parse("{}"), value);
        }
    }
}
