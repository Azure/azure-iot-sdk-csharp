﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientPropertiesTests
    {
        private const string BoolPropertyName = "boolPropertyName";
        private const string DoublePropertyName = "doublePropertyName";
        private const string FloatPropertyName = "floatPropertyName";
        private const string StringPropertyName = "stringPropertyName";
        private const string ObjectPropertyName = "objectPropertyName";
        private const string MapPropertyName = "mapPropertyName";

        private const bool BoolPropertyValue = false;
        private const double DoublePropertyValue = 1.001;
        private const float FloatPropertyValue = 1.2f;
        private const string StringPropertyValue = "propertyValue";

        private const string ComponentName = "testableComponent";

        private static readonly CustomClientProperty s_objectPropertyValue = new CustomClientProperty { Id = 123, Name = "testName" };

        private static readonly Dictionary<string, object> s_mapPropertyValue = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "key3", s_objectPropertyValue }
        };

        [TestMethod]
        public void ClientPropertyCollection_CanEnumerateClientProperties()
        {
            // arrange
            var deviceReportedProperties = new ClientPropertyCollection();
            deviceReportedProperties.AddRootProperty(StringPropertyName, StringPropertyValue);
            deviceReportedProperties.AddRootProperty(ObjectPropertyName, s_objectPropertyValue);
            deviceReportedProperties.AddComponentProperty(ComponentName, BoolPropertyName, BoolPropertyValue);

            var serviceUpdateRequestedComponentProperty = new Dictionary<string, object>
            {
                { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                { FloatPropertyName, FloatPropertyValue }
            };
            var serviceUpdateRequestedPropertiesDictionary = new Dictionary<string, object>
            {
                { DoublePropertyName, DoublePropertyValue },
                { MapPropertyName, s_mapPropertyValue },
                { ComponentName, serviceUpdateRequestedComponentProperty},
                { "$version", (long)2 }
            };

            // The service update requested properties are always deserialized into a dictionary using Newtonsoft.Json.
            // So, even though we have a dictionary object here for testing, we'll need to serialize it and deserialize it back using Newtonsoft.Json.
            string serializedServiceUpdateRequestedPropertiesDictionary = JsonConvert.SerializeObject(serviceUpdateRequestedPropertiesDictionary);
            var dictInput = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedServiceUpdateRequestedPropertiesDictionary);
            var serviceUpdateRequestedProperties = new WritableClientPropertyCollection(dictInput, DefaultPayloadConvention.Instance);

            // act
            // The test makes a call to the internal constructor.
            // The users will retrieve client properties by calling DeviceClient.GetClientProperties()/ ModuleClient.GetClientProperties().
            var clientProperties = new ClientProperties(serviceUpdateRequestedProperties, deviceReportedProperties);

            // assert
            // These are the device reported property values.
            foreach (ClientProperty deviceReportedClientProperty in clientProperties.ReportedByClient)
            {
                if (deviceReportedClientProperty.PropertyName == StringPropertyName)
                {
                    deviceReportedClientProperty.Value.Should().Be(StringPropertyValue);
                }
                else if (deviceReportedClientProperty.PropertyName == ObjectPropertyName)
                {
                    deviceReportedClientProperty.Value.Should().BeEquivalentTo(s_objectPropertyValue);
                }
                else if (deviceReportedClientProperty.ComponentName == ComponentName && deviceReportedClientProperty.PropertyName == BoolPropertyName)
                {
                    deviceReportedClientProperty.TryGetValue(out bool outValue).Should().BeTrue();
                    outValue.Should().Be(BoolPropertyValue);
                }
            }

            // These are the property values for which service has requested an update.
            foreach (WritableClientProperty updateRequesteProperties in clientProperties.WritablePropertyRequests)
            {
                if (updateRequesteProperties.PropertyName == DoublePropertyName)
                {
                    updateRequesteProperties.TryGetValue(out double value).Should().BeTrue();
                    value.Should().Be(DoublePropertyValue);
                }
                else if (updateRequesteProperties.PropertyName == MapPropertyName)
                {
                    updateRequesteProperties.TryGetValue(out Dictionary<string, object> value).Should().BeTrue();
                    value.Should().HaveSameCount(s_mapPropertyValue);

                    // TryGetValue doesn't have nested deserialization, so we'll have to serialize the retrieved value to compare with the input
                    string expectedMapPropertyValue = JsonConvert.SerializeObject(s_mapPropertyValue);
                    string actualMapPropertyValue = JsonConvert.SerializeObject(value);
                    expectedMapPropertyValue.Should().Be(actualMapPropertyValue);
                }
                else if (updateRequesteProperties.ComponentName == ComponentName && updateRequesteProperties.PropertyName == FloatPropertyName)
                {
                    (updateRequesteProperties).TryGetValue(out float value).Should().BeTrue();
                    value.Should().Be(FloatPropertyValue);
                }
            }
        }
    }
}
