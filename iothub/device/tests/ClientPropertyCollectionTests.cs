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
        private const string propertyName = "propertyOne";
        private const string simplePropertyValue = "propertyValue";
        private const string componentName = "testableComponent";
        private const string writablePropertyDescription = "testableWritablePropertyDescription";
        private const string updatedPropertyValue = "updatedPropertyValue";

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { propertyName, simplePropertyValue }
            };
            clientProperties.TryGetValue<string>(propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, propertyName, simplePropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { propertyName, simplePropertyValue }
            };
            clientProperties.TryGetValue<string>(propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");

            clientProperties.AddOrUpdate(propertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(propertyName, out var outValueChanged);
            Assert.AreEqual(updatedPropertyValue, outValueChanged, $"Property values do not match, expected {updatedPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, propertyName, simplePropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");

            clientProperties.AddOrUpdate(componentName, propertyName, updatedPropertyValue);
            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValueChanged);
            Assert.AreEqual(updatedPropertyValue, outValueChanged, $"Property values do not match, expected {updatedPropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { propertyName, simplePropertyValue }
            };
            Assert.ThrowsException<ArgumentException>(() =>
            {
                clientProperties.Add(propertyName, simplePropertyValue);
            });
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectWithComponentAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, propertyName, simplePropertyValue }
            };
            Assert.ThrowsException<ArgumentException>(() =>
            {
                clientProperties.Add(componentName, propertyName, simplePropertyValue);
            });
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleWritablePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { propertyName, simplePropertyValue, 200, 2, writablePropertyDescription }
            };
            clientProperties.TryGetValue<dynamic>(propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue.value, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(200, outValue.ac, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(2, outValue.av, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(writablePropertyDescription, outValue.ad, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddWritablePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, propertyName, simplePropertyValue, 200, 2, writablePropertyDescription }
            };
            clientProperties.TryGetValue<dynamic>(componentName, propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue.value, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(200, outValue.ac, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(2, outValue.av, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual(writablePropertyDescription, outValue.ad, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollection_AddingComponentAddsComponentIdentifier()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { componentName, propertyName, simplePropertyValue }
            };
            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValue);
            clientProperties.TryGetValue<string>(componentName, "__t", out var componentOut);

            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual("c", componentOut, $"Property values do not match, expected c but got {componentOut}");
        }
    }
}