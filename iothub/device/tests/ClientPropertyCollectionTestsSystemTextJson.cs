using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Tests
{
#if !NET451 // System.Text.Json is not available here.
    public class SystemTextJsonConvention : PayloadConvention
    {
        public override PayloadSerializer PayloadSerializer => new SystemTextJsonPayloadSerializer();

        public override PayloadEncoder PayloadEncoder => new Utf8PayloadEncoder();
    }

    [TestClass]
    [TestCategory("Unit")]
    public class ClientPropertyCollectionTestsSystemTextJson
    {
        private const string propertyName = "propertyOne";
        private const string simplePropertyValue = "propertyValue";
        private const string componentName = "testableComponent";
        private const string writablePropertyDescription = "testableWritablePropertyDescription";
        private const string updatedPropertyValue = "updatedPropertyValue";
        
        private const string twinJson = "{ \"propertyOne\" : \"propertyValue\" }";
        private const string twinJsonWritableProperty = "{ \"propertyOne\" : { \"value\" : \"propertyValue\",  \"ac\" : 200, \"av\" : 2, \"ad\" : \"testableWritablePropertyDescription\" } }";
        private const string twinJsonWithComponent = "{ \"testableComponent\" : { \"__t\" : \"c\",  \"propertyOne\" : \"propertyValue\" } }";
        private const string twinJsonWritablePropertyWithComponent = "{ \"testableComponent\" : { \"__t\" : \"c\", \"propertyOne\" : { \"value\" : \"propertyValue\",  \"ac\" : 200, \"av\" : 2, \"ad\" : \"testableWritablePropertyDescription\" } } }";

        private static TwinCollection collectionToRoundTrip = new TwinCollection(twinJson);
        private static TwinCollection collectionWritablePropertyToRoundTrip = new TwinCollection(twinJsonWritableProperty);
        private static TwinCollection collectionWithComponentToRoundTrip = new TwinCollection(twinJsonWithComponent);
        private static TwinCollection collectionWritablePropertyWithComponentToRoundTrip = new TwinCollection(twinJsonWritablePropertyWithComponent);

        [TestMethod]
        public void ClientPropertyCollectionSystemTextJson_CanGetSimpleValue()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, new SystemTextJsonConvention());
            clientProperties.Convention = new SystemTextJsonConvention();
            clientProperties.TryGetValue<string>(propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollectionSystemTextJson_CanGetSimpleValueWithComponent()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, new SystemTextJsonConvention());
            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
        }

        [TestMethod]
        public void ClientPropertyCollectionSystemTextJson_CanAddSimpleWritablePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyToRoundTrip, new SystemTextJsonConvention());

            clientProperties.TryGetValue<SystemTextJsonWritablePropertyResponse>(propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, outValue.Value, $"Property values do not match, expected {simplePropertyValue} but got {outValue.Value}");
            Assert.AreEqual(200, outValue.AckCode, $"Property values do not match, expected 200 but got {outValue.AckCode}");
            Assert.AreEqual(2, outValue.AckVersion, $"Property values do not match, expected 2 but got {outValue.AckVersion}");
            Assert.AreEqual(writablePropertyDescription, outValue.AckDescription, $"Property values do not match, expected {writablePropertyDescription} but got {outValue.AckDescription}");
        }

        [TestMethod]
        public void ClientPropertyCollectionSystemTextJson_CanAddWritablePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyWithComponentToRoundTrip, new SystemTextJsonConvention());

            clientProperties.TryGetValue<SystemTextJsonWritablePropertyResponse>(componentName, propertyName, out var outValue);
            Assert.AreEqual(simplePropertyValue, (string)outValue.Value, $"Property values do not match, expected {simplePropertyValue} but got {outValue.Value}");
            Assert.AreEqual(200, outValue.AckCode, $"Property values do not match, expected 200 but got {outValue.AckCode}");
            Assert.AreEqual(2, outValue.AckVersion, $"Property values do not match, expected 2 but got {outValue.AckVersion}");
            Assert.AreEqual(writablePropertyDescription, outValue.AckDescription, $"Property values do not match, expected {writablePropertyDescription} but got {outValue.AckDescription}");
        }

        [TestMethod]
        public void ClientPropertyCollectionSystemTextJson_CanGetComponentIdentifier()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, new SystemTextJsonConvention());

            clientProperties.TryGetValue<string>(componentName, propertyName, out var outValue);
            clientProperties.TryGetValue<string>(componentName, "__t", out var componentOut);

            Assert.AreEqual(simplePropertyValue, outValue, $"Property values do not match, expected {simplePropertyValue} but got {outValue}");
            Assert.AreEqual("c", componentOut, $"Property values do not match, expected c but got {componentOut}");
        }
    }
#endif
}