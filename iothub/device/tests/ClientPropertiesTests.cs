// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            var serviceUpdateRequestedProperties = new ClientPropertyCollection();
            serviceUpdateRequestedProperties.AddRootProperty(DoublePropertyName, DoublePropertyValue);
            serviceUpdateRequestedProperties.AddRootProperty(MapPropertyName, s_mapPropertyValue);
            serviceUpdateRequestedProperties.AddComponentProperty(ComponentName, FloatPropertyName, FloatPropertyValue);

            // act
            // The test makes a call to the internal constructor.
            // The users will retrieve client properties by calling DeviceClient.GetClientProperties()/ ModuleClient.GetClientProperties().
            var clientProperties = new ClientProperties(serviceUpdateRequestedProperties, deviceReportedProperties);

            // assert
            // These are the device reported property values.
            foreach (var deviceReportedKeyValuePairs in clientProperties.ReportedFromClient)
            {
                if (deviceReportedKeyValuePairs.Key.Equals(StringPropertyName))
                {
                    deviceReportedKeyValuePairs.Value.Should().Be(StringPropertyValue);
                }
                else if (deviceReportedKeyValuePairs.Key.Equals(ObjectPropertyName))
                {
                    deviceReportedKeyValuePairs.Value.Should().BeEquivalentTo(s_objectPropertyValue);
                }
                else if (deviceReportedKeyValuePairs.Key.Equals(ComponentName))
                {
                    deviceReportedKeyValuePairs.Value.Should().BeOfType(typeof(Dictionary<string, object>));
                    var componentDictionary = deviceReportedKeyValuePairs.Value;

                    componentDictionary.As<Dictionary<string, object>>().TryGetValue(BoolPropertyName, out object outValue).Should().BeTrue();
                    outValue.Should().Be(BoolPropertyValue);
                }
            }

            // These are the property values for which service has requested an update.
            foreach (var updateRequestedKeyValuePairs in clientProperties.WritablePropertyRequests)
            {
                if (updateRequestedKeyValuePairs.Key.Equals(DoublePropertyName))
                {
                    updateRequestedKeyValuePairs.Value.Should().Be(DoublePropertyValue);
                }
                else if (updateRequestedKeyValuePairs.Key.Equals(MapPropertyName))
                {
                    updateRequestedKeyValuePairs.Value.Should().BeEquivalentTo(s_mapPropertyValue);
                }
                else if (updateRequestedKeyValuePairs.Key.Equals(ComponentName))
                {
                    updateRequestedKeyValuePairs.Value.Should().BeOfType(typeof(Dictionary<string, object>));
                    var componentDictionary = updateRequestedKeyValuePairs.Value;

                    componentDictionary.As<Dictionary<string, object>>().TryGetValue(FloatPropertyName, out object outValue).Should().BeTrue();
                    outValue.Should().Be(FloatPropertyValue);
                }
            }
        }
    }
}
