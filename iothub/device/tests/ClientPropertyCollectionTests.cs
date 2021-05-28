// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using FluentAssertions;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientPropertyCollectionTests
    {
        private const string boolPropertyName = "boolProperty";
        private const string doublePropertyName = "doubleProperty";
        private const string floatPropertyName = "floatProperty";
        private const string intPropertyName = "intProperty";
        private const string shortPropertyName = "shortProperty";
        private const string stringPropertyName = "stringPropertyName";
        private const bool boolPropertyValue = false;
        private const double doublePropertyValue = 1.001;
        private const float floatPropertyValue = 1.2f;
        private const int intPropertyValue = 12345678;
        private const short shortPropertyValue = 1234;
        private const string stringPropertyValue = "propertyValue";
        private const string componentName = "testableComponent";
        private const string writablePropertyDescription = "testableWritablePropertyDescription";
        private const string updatedPropertyValue = "updatedPropertyValue";

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectsAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue },
                { boolPropertyName, boolPropertyValue },
                { doublePropertyName, doublePropertyValue },
                { floatPropertyName, floatPropertyValue },
                { intPropertyName, intPropertyValue },
                { shortPropertyName, shortPropertyValue }
            };

            clientProperties.TryGetValue(stringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(stringPropertyValue);

            clientProperties.TryGetValue(boolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(boolPropertyValue);

            clientProperties.TryGetValue(doublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(doublePropertyValue);

            clientProperties.TryGetValue(floatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(floatPropertyValue);

            clientProperties.TryGetValue(intPropertyName, out int intOutValue);
            intOutValue.Should().Be(intPropertyValue);

            clientProperties.TryGetValue(shortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(shortPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue }
            };

            Action act = () => clientProperties.Add(stringPropertyName, stringPropertyValue);
            act.Should().Throw<ArgumentException>("\"Add\" method does not support adding a key that already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue }
            };
            clientProperties.TryGetValue<string>(stringPropertyName, out var outValue);
            outValue.Should().Be(stringPropertyValue);

            clientProperties.AddOrUpdate(stringPropertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(stringPropertyName, out var outValueChanged);
            outValueChanged.Should().Be(updatedPropertyValue, "\"AddOrUpdate\" should overwrite the value if the key already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddMultiplePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.Add(stringPropertyName, stringPropertyValue);
            clientProperties.Add(intPropertyName, intPropertyValue);

            clientProperties.TryGetValue<string>(stringPropertyName, out var outStringValue);
            outStringValue.Should().Be(stringPropertyValue);

            clientProperties.TryGetValue<int>(intPropertyName, out var outIntValue);
            outIntValue.Should().Be(intPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, new Dictionary<string, object> {
                    {stringPropertyName, stringPropertyValue },
                    {boolPropertyName, boolPropertyValue },
                    {doublePropertyName, doublePropertyValue },
                    {floatPropertyName, floatPropertyValue },
                    {intPropertyName, intPropertyValue },
                    {shortPropertyName, shortPropertyValue } }
                }
            };

            clientProperties.TryGetValue(componentName, stringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(stringPropertyValue);

            clientProperties.TryGetValue(componentName, boolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(boolPropertyValue);

            clientProperties.TryGetValue(componentName, doublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(doublePropertyValue);

            clientProperties.TryGetValue(componentName, floatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(floatPropertyValue);

            clientProperties.TryGetValue(componentName, intPropertyName, out int intOutValue);
            intOutValue.Should().Be(intPropertyValue);

            clientProperties.TryGetValue(componentName, shortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(shortPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectWithComponentAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue },
            };

            Action act = () => clientProperties.Add(componentName, stringPropertyName, stringPropertyValue);
            act.Should().Throw<ArgumentException>("\"Add\" method does not support adding a key that already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValue);
            outValue.Should().Be(stringPropertyValue);

            clientProperties.AddOrUpdate(componentName, stringPropertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValueChanged);
            outValueChanged.Should().Be(updatedPropertyValue, "\"AddOrUpdate\" should overwrite the value if the key already exists in the collection.");
        }

        // Component1   -> prop1 = 123
        //              -> prop2 = "abc"
        // How to differentiate between:
        //  -> replace value of prop2 => AddOrUpdate("Component1", "prop2", "xyz");
        // vs
        //  -> replace the value for Component1 altogether

        [TestMethod]
        public void ClientPropertyCollection_CanAddMultiplePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.Add(componentName, stringPropertyName, stringPropertyValue);
            clientProperties.Add(componentName, intPropertyName, intPropertyValue);

            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outStringValue);
            outStringValue.Should().Be(stringPropertyValue);

            clientProperties.TryGetValue<int>(componentName, intPropertyName, out var outIntValue);
            outIntValue.Should().Be(intPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleWritablePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue, 200, 2, writablePropertyDescription }
            };
            clientProperties.TryGetValue<dynamic>(stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue.value, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(200, outValue.ac, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(2, outValue.av, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(writablePropertyDescription, outValue.ad, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddWritablePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue, 200, 2, writablePropertyDescription }
            };
            clientProperties.TryGetValue<dynamic>(componentName, stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue.value, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(200, outValue.ac, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(2, outValue.av, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(writablePropertyDescription, outValue.ad, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_AddingComponentAddsComponentIdentifier()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValue);
            clientProperties.TryGetValue<string>(componentName, ConventionBasedConstants.ComponentIdentifierKey, out var componentOut);

            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual(ConventionBasedConstants.ComponentIdentifierValue, componentOut, $"Property values do not match, expected c but got {componentOut}");
        }
    }
}
