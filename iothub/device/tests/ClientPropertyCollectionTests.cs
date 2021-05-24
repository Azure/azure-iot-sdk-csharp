using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;

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
            Assert.AreEqual(stringPropertyValue, stringOutValue, $"Property values do not match, expected {stringPropertyValue} but got {stringOutValue}");
            clientProperties.TryGetValue(boolPropertyName, out bool boolOutValue);
            Assert.AreEqual(boolPropertyValue, boolOutValue, $"Property values do not match, expected {boolPropertyValue} but got {boolOutValue}");
            clientProperties.TryGetValue(doublePropertyName, out double doubleOutValue);
            Assert.AreEqual(doublePropertyValue, doubleOutValue, $"Property values do not match, expected {doublePropertyValue} but got {doubleOutValue}");
            clientProperties.TryGetValue(floatPropertyName, out float floatOutValue);
            Assert.AreEqual(floatPropertyValue, floatOutValue, $"Property values do not match, expected {floatPropertyValue} but got {floatOutValue}");
            clientProperties.TryGetValue(intPropertyName, out int intOutValue);
            Assert.AreEqual(intPropertyValue, intOutValue, $"Property values do not match, expected {intPropertyValue} but got {intOutValue}");
            clientProperties.TryGetValue(shortPropertyName, out short shortOutValue);
            Assert.AreEqual(shortPropertyValue, shortOutValue, $"Property values do not match, expected {shortPropertyValue} but got {shortOutValue}");
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
            clientProperties.TryGetValue(componentName, stringPropertyName, out string outValue);
            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            clientProperties.TryGetValue(componentName, boolPropertyName, out bool boolOutValue);
            Assert.AreEqual(boolPropertyValue, boolOutValue, $"Property values do not match, expected {boolPropertyValue} but got {boolOutValue}");
            clientProperties.TryGetValue(componentName, doublePropertyName, out double doubleOutValue);
            Assert.AreEqual(doublePropertyValue, doubleOutValue, $"Property values do not match, expected {doublePropertyValue} but got {doubleOutValue}");
            clientProperties.TryGetValue(componentName, floatPropertyName, out float floatOutValue);
            Assert.AreEqual(floatPropertyValue, floatOutValue, $"Property values do not match, expected {floatPropertyValue} but got {floatOutValue}");
            clientProperties.TryGetValue(componentName, intPropertyName, out int intOutValue);
            Assert.AreEqual(intPropertyValue, intOutValue, $"Property values do not match, expected {intPropertyValue} but got {intOutValue}");
            clientProperties.TryGetValue(componentName, shortPropertyName, out short shortOutValue);
            Assert.AreEqual(shortPropertyValue, shortOutValue, $"Property values do not match, expected {shortPropertyValue} but got {shortOutValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue }
            };
            clientProperties.TryGetValue<string>(stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");

            clientProperties.AddOrUpdate(stringPropertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(stringPropertyName, out var outValueChanged);
            Assert.AreEqual(updatedPropertyValue, outValueChanged, $"Property values do not match, expected {updatedPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");

            clientProperties.AddOrUpdate(componentName, stringPropertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValueChanged);
            Assert.AreEqual(updatedPropertyValue, outValueChanged, $"Property values do not match, expected {updatedPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { stringPropertyName, stringPropertyValue }
            };
            Assert.ThrowsException<ArgumentException>(() =>
            {
                clientProperties.Add(stringPropertyName, stringPropertyValue);
            });
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectWithComponentAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, stringPropertyName, stringPropertyValue },

            };
            Assert.ThrowsException<ArgumentException>(() =>
            {
                clientProperties.Add(componentName, stringPropertyName, stringPropertyValue);
            });
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