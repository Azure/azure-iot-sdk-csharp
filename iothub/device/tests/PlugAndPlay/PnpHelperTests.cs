// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Devices.Client.PlugAndPlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Tests.PlugAndPlay
{
    /// <summary>
    /// Ensures the PnP convention helpers produce content to spec and using different data types
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class PnpHelperTests
    {
        [TestMethod]
        public void CreateRootPropertyPatch()
        {
            // Format:
            //  {
            //      "samplePropertyName": 20
            //  }

            const string propertyName = "someName";
            const int propertyValue = 10;

            string patch = PnpHelper.CreatePropertyPatch(propertyName, JsonConvert.SerializeObject(propertyValue));
            var jObject = JObject.Parse(patch);

            jObject.Count.Should().Be(1, "there should be a single property added");
            jObject.Value<int>(propertyName).Should().Be(propertyValue);
        }

        [TestMethod]
        public void CreateComponentPropertyPatch()
        {
            // Format:
            //  {
            //      "sampleComponentName": {
            //          "__t": "c",
            //          "samplePropertyName"": 20
            //      }
            //  }

            const string componentName = "someComponent";
            const string propertyName = "someName";
            const int propertyValue = 10;

            string patch = PnpHelper.CreatePropertyPatch(propertyName, JsonConvert.SerializeObject(propertyValue), componentName);

            var jObject = JObject.Parse(patch);
            JObject component = jObject.Value<JObject>(componentName);

            component.Count.Should().Be(2, "there should be two properties added - the above property and a component identifier {\"__t\": \"c\"}");
            component.Value<int>(propertyName).Should().Be(propertyValue);
            component[PnpHelper.PropertyComponentIdentifierKey].Should().NotBeNull();
            ((string)component[PnpHelper.PropertyComponentIdentifierKey]).Should().Be(PnpHelper.PropertyComponentIdentifierValue);
        }

        [TestMethod]
        public void CreateRootPropertyEmbeddedValuePatch()
        {
            // Format:
            //  {
            //      "samplePropertyName": {
            //          "value": 20,
            //          "ac": 200,
            //          "av": 5,
            //          "ad": "The update was successful."
            //      }
            //  }

            const string propertyName = "someName";
            const int propertyValue = 10;
            const int ackCode = 200;
            const long ackVersion = 2;

            string patch = PnpHelper.CreatePropertyEmbeddedValuePatch(propertyName, JsonConvert.SerializeObject(propertyValue), ackCode, ackVersion);

            var jObject = JObject.Parse(patch);
            EmbeddedPropertyPatch actualPatch = jObject.ToObject<EmbeddedPropertyPatch>();

            // The property patch object should have "value", "ac" and "av" properties set. Since we did not supply an "ackDescription", "ad" should be null.
            actualPatch.Value.SerializedValue.Should().Be(JsonConvert.SerializeObject(propertyValue));
            actualPatch.Value.AckCode.Should().Be(ackCode);
            actualPatch.Value.AckVersion.Should().Be(ackVersion);
            actualPatch.Value.AckDescription.Should().BeNull();
        }

        [TestMethod]
        public void CreateComponentPropertyEmbeddedValuePatch()
        {
            // Format:
            //  {
            //      "sampleComponentName": {
            //          "__t": "c",
            //          "samplePropertyName": {
            //              "value": 20,
            //              "ac": 200,
            //              "av": 5,
            //              "ad": "The update was successful."
            //          }
            //      }
            //  }

            const string componentName = "someComponentName";
            const string propertyName = "someName";
            const int propertyValue = 10;
            const int ackCode = 200;
            const long ackVersion = 2;
            const string ackDescription = "The update was successful";

            string patch = PnpHelper.CreatePropertyEmbeddedValuePatch(
                propertyName,
                JsonConvert.SerializeObject(propertyValue),
                ackCode,
                ackVersion,
                ackDescription,
                componentName);

            var jObject = JObject.Parse(patch);
            JObject component = jObject.Value<JObject>(componentName);

            // There should be two properties added to the component- the above property and a component identifier "__t": "c".
            component.Count.Should().Be(2);
            component[PnpHelper.PropertyComponentIdentifierKey].Should().NotBeNull();
            ((string)component[PnpHelper.PropertyComponentIdentifierKey]).Should().Be(PnpHelper.PropertyComponentIdentifierValue);

            // The property patch object should have "value", "ac", "av" and "ad" properties set.
            EmbeddedPropertyPatch actualPatch = component.ToObject<EmbeddedPropertyPatch>();
            actualPatch.Value.SerializedValue.Should().Be(JsonConvert.SerializeObject(propertyValue));
            actualPatch.Value.AckCode.Should().Be(ackCode);
            actualPatch.Value.AckVersion.Should().Be(ackVersion);
            actualPatch.Value.AckDescription.Should().Be(ackDescription);
        }
    }

    internal class EmbeddedPropertyPatch
    {
        [JsonProperty("someName")]
        internal EmbeddedPropertyPatchValue Value { get; set; }
    }

    internal class EmbeddedPropertyPatchValue
    {
        [JsonProperty("value")]
        internal string SerializedValue { get; set; }

        [JsonProperty("ac")]
        internal int AckCode { get; set; }

        [JsonProperty("av")]
        internal long AckVersion { get; set; }

        [JsonProperty("ad")]
        internal string AckDescription { get; set; }
    }
}
