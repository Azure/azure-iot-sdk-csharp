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
    public class ClientPropertyCollectionTestsNewtonsoft
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
        private const string twinJson = "{ \"stringPropertyName\" : \"propertyValue\", \"boolProperty\" : false, \"doublePropertyName\" : 1.001, \"floatProperty\" : 1.2, \"intProperty\" : 12345678, \"shortProperty\" : 1234 }";
        private const string twinJsonWritableProperty = "{ \"stringPropertyName\" : { \"value\" : \"propertyValue\",  \"ac\" : 200, \"av\" : 2, \"ad\" : \"testableWritablePropertyDescription\" } }";
        private const string twinJsonWithComponent = "{ \"testableComponent\" : { \"__t\" : \"c\",  \"stringPropertyName\" : \"propertyValue\", \"boolProperty\" : false, \"doublePropertyName\" : 1.001, \"floatProperty\" : 1.2, \"intProperty\" : 12345678, \"shortProperty\" : 1234 } }";
        private const string twinJsonWritablePropertyWithComponent = "{ \"testableComponent\" : { \"__t\" : \"c\", \"stringPropertyName\" : { \"value\" : \"propertyValue\",  \"ac\" : 200, \"av\" : 2, \"ad\" : \"testableWritablePropertyDescription\" } } }";

        private static TwinCollection collectionToRoundTrip = new TwinCollection(twinJson);
        private static TwinCollection collectionWritablePropertyToRoundTrip = new TwinCollection(twinJsonWritableProperty);
        private static TwinCollection collectionWithComponentToRoundTrip = new TwinCollection(twinJsonWithComponent);
        private static TwinCollection collectionWritablePropertyWithComponentToRoundTrip = new TwinCollection(twinJsonWritablePropertyWithComponent);

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetSimpleValue()
        {
            
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, DefaultPayloadConvention.Instance);
            clientProperties.Convention = DefaultPayloadConvention.Instance;
            
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
        public void ClientPropertyCollectionNewtonsoft_CanGetSimpleValueWithComponent()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);
            clientProperties.TryGetValue(componentName, stringPropertyName, out string outValue);
            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            clientProperties.TryGetValue(componentName, boolPropertyName, out bool boolOutValue);
            Assert.AreEqual(boolPropertyValue, boolOutValue, $"Property values do not match, expected {boolPropertyValue} but got {boolOutValue}");
            // clientProperties.TryGetValue(componentName, doublePropertyName, out double doubleOutValue);
            // Assert.AreEqual(doublePropertyValue, doubleOutValue, $"Property values do not match, expected {doublePropertyValue} but got {doubleOutValue}");
            clientProperties.TryGetValue(componentName, floatPropertyName, out float floatOutValue);
            Assert.AreEqual(floatPropertyValue, floatOutValue, $"Property values do not match, expected {floatPropertyValue} but got {floatOutValue}");
            clientProperties.TryGetValue(componentName, intPropertyName, out int intOutValue);
            Assert.AreEqual(intPropertyValue, intOutValue, $"Property values do not match, expected {intPropertyValue} but got {intOutValue}");
            clientProperties.TryGetValue(componentName, shortPropertyName, out short shortOutValue);
            Assert.AreEqual(shortPropertyValue, shortOutValue, $"Property values do not match, expected {shortPropertyValue} but got {shortOutValue}");
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanAddSimpleWritablePropertyAndGetBack()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyToRoundTrip, DefaultPayloadConvention.Instance);

            clientProperties.TryGetValue<NewtonsoftJsonWritablePropertyResponse>(stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue.Value, $"Property values do not match, expected {stringPropertyValue} but got {outValue.Value}");
            Assert.AreEqual(200, outValue.AckCode, $"Property values do not match, expected 200 but got {outValue.AckCode}");
            Assert.AreEqual(2, outValue.AckVersion, $"Property values do not match, expected 2 but got {outValue.AckVersion}");
            Assert.AreEqual(writablePropertyDescription, outValue.AckDescription, $"Property values do not match, expected {writablePropertyDescription} but got {outValue.AckDescription}");
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanAddWritablePropertyWithComponentAndGetBack()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            clientProperties.TryGetValue<NewtonsoftJsonWritablePropertyResponse>(componentName, stringPropertyName, out var outValue);
            Assert.AreEqual(stringPropertyValue, outValue.Value, $"Property values do not match, expected {stringPropertyValue} but got {outValue.Value}");
            Assert.AreEqual(200, outValue.AckCode, $"Property values do not match, expected 200 but got {outValue.AckCode}");
            Assert.AreEqual(2, outValue.AckVersion, $"Property values do not match, expected 2 but got {outValue.AckVersion}");
            Assert.AreEqual(writablePropertyDescription, outValue.AckDescription, $"Property values do not match, expected {writablePropertyDescription} but got {outValue.AckDescription}");
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetComponentIdentifier()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            clientProperties.TryGetValue<string>(componentName, stringPropertyName, out var outValue);
            clientProperties.TryGetValue<string>(componentName, "__t", out var componentOut);

            Assert.AreEqual(stringPropertyValue, outValue, $"Property values do not match, expected {stringPropertyValue} but got {outValue}");
            Assert.AreEqual("c", componentOut, $"Property values do not match, expected c but got {componentOut}");
        }
    }
}