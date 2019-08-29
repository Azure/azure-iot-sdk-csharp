// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pose;

namespace Azure.IoT.DigitalTwin.Device.Test.Model
{
    [TestClass]
    public class DataCollectionTest
    {
        [TestMethod]
        public void TestDefaultConstructor()
        {
            var dc = new DataCollection();

            Assert.AreEqual(new JObject().ToString(), dc.JObject.ToString());
        }

        [TestMethod]
        public void TestConstructorWithPropertiesJsonWhenGivenNull()
        {
            try
            {
                var dc = new DataCollection(null);
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("propertiesJson", ex.ParamName);
                return;
            }

            Assert.Fail("Expect exception thrown, but not happened.");
        }

        [TestMethod]
        public void TestConstructorWithPropertiesJsonWhenGivenWhiteSpace()
        {
            try
            {
                var dc = new DataCollection(" ");
            }
            catch (ArgumentNullException ex)
            {
                Assert.AreEqual("propertiesJson", ex.ParamName);
                return;
            }

            Assert.Fail("Expect exception thrown, but not happened.");
        }

        [TestMethod]
        public void TestConstructorWithPropertiesJson()
        {
            string json = "{\"key1\": \"value1\", \"key2\": { \"value\": 123 } }";
            var dc = new DataCollection(json);

            Assert.AreEqual(JObject.Parse(json).ToString(), dc.JObject.ToString());
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject jObj = new JObject();
            jObj.Add("Key1", 999);
            jObj.Add("Key2", "OneTwoThree");
            string json = JsonConvert.SerializeObject(jObj);

            var dataCollection = new DataCollection(json);
            string actualJson = dataCollection.ToJson();
            Assert.AreEqual(json, actualJson);
        }

        [TestMethod]
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
                    Assert.AreEqual(999, (long)((JValue)item.Value).Value);
                    count++;
                }
                else if (item.Key == "Key2")
                {
                    Assert.AreEqual("OneTwoThree", (string)((JValue)item.Value).Value);
                    count++;
                }
                else
                {
                    Assert.Fail("data collection enumerate with unexpected key.");
                }
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void TestToString()
        {
            JObject jObj = new JObject();
            jObj.Add("Key1", 999);
            jObj.Add("Key2", "OneTwoThree");
            string json = JsonConvert.SerializeObject(jObj, Formatting.Indented);

            var dataCollection = new DataCollection(json);
            string actualJson = dataCollection.ToString();
            Assert.AreEqual(json, actualJson);
        }
    }
}
