// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    // These tests test the deserialization of the service response to a ClientPropertyCollection.
    // This flow is convention aware and uses NewtonSoft.Json for deserialization.
    // For the purpose of these tests we will create an instance of a Twin class to simulate the service response.
    public class ClientPropertyCollectionTestsNewtonsoft
    {
        internal const string BoolPropertyName = "boolPropertyName";
        internal const string DoublePropertyName = "doublePropertyName";
        internal const string FloatPropertyName = "floatPropertyName";
        internal const string IntPropertyName = "intPropertyName";
        internal const string ShortPropertyName = "shortPropertyName";
        internal const string StringPropertyName = "stringPropertyName";
        internal const string ObjectPropertyName = "objectPropertyName";
        internal const string ArrayPropertyName = "arrayPropertyName";
        internal const string MapPropertyName = "mapPropertyName";
        internal const string DateTimePropertyName = "dateTimePropertyName";
        internal const string ComponentName = "testableComponent";

        private const bool BoolPropertyValue = false;
        private const double DoublePropertyValue = 1.001;
        private const float FloatPropertyValue = 1.2f;
        private const int IntPropertyValue = 12345678;
        private const short ShortPropertyValue = 1234;
        private const string StringPropertyValue = "propertyValue";

        private const string UpdatedPropertyValue = "updatedPropertyValue";

        private static readonly DateTimeOffset s_dateTimePropertyValue = DateTimeOffset.Now;
        private static readonly CustomClientProperty s_objectPropertyValue = new CustomClientProperty { Id = 123, Name = "testName" };

        private static readonly List<object> s_arrayPropertyValue = new List<object>
        {
            1,
            "someString",
            false,
            s_objectPropertyValue
        };

        private static readonly Dictionary<string, object> s_mapPropertyValue = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 123 },
            { "key3", s_objectPropertyValue }
        };

        // Create an object that represents all of the properties as top-level properties.
        private static readonly RootLevelProperties s_rootLevelProperties = new RootLevelProperties
        {
            BooleanProperty = BoolPropertyValue,
            DoubleProperty = DoublePropertyValue,
            FloatProperty = FloatPropertyValue,
            IntProperty = IntPropertyValue,
            ShortProperty = ShortPropertyValue,
            StringProperty = StringPropertyValue,
            ObjectProperty = s_objectPropertyValue,
            ArrayProperty = s_arrayPropertyValue,
            MapProperty = s_mapPropertyValue,
            DateTimeProperty = s_dateTimePropertyValue
        };

        // Create an object that represents all of the properties as component-level properties.
        // This adds the "__t": "c" component identifier as a part of "ComponentProperties" class declaration.
        private static readonly ComponentLevelProperties s_componentLevelProperties = new ComponentLevelProperties
        {
            Properties = new ComponentProperties
            {
                BooleanProperty = BoolPropertyValue,
                DoubleProperty = DoublePropertyValue,
                FloatProperty = FloatPropertyValue,
                IntProperty = IntPropertyValue,
                ShortProperty = ShortPropertyValue,
                StringProperty = StringPropertyValue,
                ObjectProperty = s_objectPropertyValue,
                ArrayProperty = s_arrayPropertyValue,
                MapProperty = s_mapPropertyValue,
                DateTimeProperty = s_dateTimePropertyValue
            },
        };

        // Create a writable property response with the expected values.
        private static readonly IWritablePropertyResponse s_writablePropertyResponse = new NewtonsoftJsonWritablePropertyResponse(
            propertyValue: StringPropertyValue,
            ackCode: CommonClientResponseCodes.OK,
            ackVersion: 2,
            ackDescription: "testableWritablePropertyDescription");

        // Create a JObject instance that represents a writable property response sent for a top-level property.
        private static readonly JObject s_writablePropertyResponseJObject = new JObject(
            new JProperty(StringPropertyName, JObject.FromObject(s_writablePropertyResponse)));

        // Create a JObject instance that represents a writable property response sent for a component-level property.
        // This adds the "__t": "c" component identifier to the constructed JObject.
        private static readonly JObject s_writablePropertyResponseWithComponentJObject = new JObject(
            new JProperty(ComponentName, new JObject(
                new JProperty(ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue),
                new JProperty(StringPropertyName, JObject.FromObject(s_writablePropertyResponse)))));

        // The above constructed json objects are used for initializing a twin response.
        // This is because we are using a Twin instance to simulate the service response.

        private static TwinCollection collectionToRoundTrip = new TwinCollection(JsonConvert.SerializeObject(s_rootLevelProperties));
        private static TwinCollection collectionWithComponentToRoundTrip = new TwinCollection(JsonConvert.SerializeObject(s_componentLevelProperties));
        private static TwinCollection collectionWritablePropertyToRoundTrip = new TwinCollection(s_writablePropertyResponseJObject, null);
        private static TwinCollection collectionWritablePropertyWithComponentToRoundTrip = new TwinCollection(s_writablePropertyResponseWithComponentJObject, null);

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetValue()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, DefaultPayloadConvention.Instance);

            // act, assert

            clientProperties.TryGetValue(StringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(BoolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(BoolPropertyValue);

            clientProperties.TryGetValue(DoublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(DoublePropertyValue);

            clientProperties.TryGetValue(FloatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(FloatPropertyValue);

            clientProperties.TryGetValue(IntPropertyName, out int intOutValue);
            intOutValue.Should().Be(IntPropertyValue);

            clientProperties.TryGetValue(ShortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(ShortPropertyValue);

            clientProperties.TryGetValue(ObjectPropertyName, out CustomClientProperty objectOutValue);
            objectOutValue.Id.Should().Be(s_objectPropertyValue.Id);
            objectOutValue.Name.Should().Be(s_objectPropertyValue.Name);

            // The two lists won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the list is deserialized to a JObject.
            clientProperties.TryGetValue(ArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);

            // The two dictionaries won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the dictionary is deserialized to a JObject.
            clientProperties.TryGetValue(MapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);

            clientProperties.TryGetValue(DateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetValueWithComponent()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            // act, assert

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(ComponentName, BoolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(BoolPropertyValue);

            clientProperties.TryGetValue(ComponentName, DoublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(DoublePropertyValue);

            clientProperties.TryGetValue(ComponentName, FloatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(FloatPropertyValue);

            clientProperties.TryGetValue(ComponentName, IntPropertyName, out int intOutValue);
            intOutValue.Should().Be(IntPropertyValue);

            clientProperties.TryGetValue(ComponentName, ShortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(ShortPropertyValue);

            clientProperties.TryGetValue(ComponentName, ObjectPropertyName, out CustomClientProperty objectOutValue);
            objectOutValue.Id.Should().Be(s_objectPropertyValue.Id);
            objectOutValue.Name.Should().Be(s_objectPropertyValue.Name);

            // The two lists won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the list is deserialized to a JObject.
            clientProperties.TryGetValue(ComponentName, ArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);

            // The two dictionaries won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the dictionary is deserialized to a JObject.
            clientProperties.TryGetValue(ComponentName, MapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);

            clientProperties.TryGetValue(ComponentName, DateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanAddSimpleWritablePropertyAndGetBack()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(StringPropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);

            // assert
            outValue.Value.Should().Be(StringPropertyValue);
            outValue.AckCode.Should().Be(s_writablePropertyResponse.AckCode);
            outValue.AckVersion.Should().Be(s_writablePropertyResponse.AckVersion);
            outValue.AckDescription.Should().Be(s_writablePropertyResponse.AckDescription);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanAddWritablePropertyWithComponentAndGetBack()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWritablePropertyWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(ComponentName, StringPropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);

            // assert
            outValue.Value.Should().Be(StringPropertyValue);
            outValue.AckCode.Should().Be(s_writablePropertyResponse.AckCode);
            outValue.AckVersion.Should().Be(s_writablePropertyResponse.AckVersion);
            outValue.AckDescription.Should().Be(s_writablePropertyResponse.AckDescription);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetComponentIdentifier()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outValue);
            clientProperties.TryGetValue(ComponentName, ConventionBasedConstants.ComponentIdentifierKey, out string componentOut);

            // assert
            outValue.Should().Be(StringPropertyValue);
            componentOut.Should().Be(ConventionBasedConstants.ComponentIdentifierValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueShouldReturnFalseIfValueNotFound()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue("thisPropertyDoesNotExist", out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueWithComponentShouldReturnFalseIfValueNotFound()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(ComponentName, "thisPropertyDoesNotExist", out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, DefaultPayloadConvention.Instance);

            bool isValueRetrieved = clientProperties.TryGetValue(StringPropertyName, out int outIntValue);
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueWithComponentShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionWithComponentToRoundTrip, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(ComponentName, StringPropertyName, out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueWithComponentShouldReturnFalseIfNotAComponent()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromTwinCollection(collectionToRoundTrip, DefaultPayloadConvention.Instance);
            string incorrectlyMappedComponentName = MapPropertyName;
            string incorrectlyMappedComponentPropertyName = "key1";

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(incorrectlyMappedComponentName, incorrectlyMappedComponentPropertyName, out object propertyValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            propertyValue.Should().Be(default);
        }
    }

    internal class RootLevelProperties
    {
        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.BoolPropertyName)]
        public bool BooleanProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.DoublePropertyName)]
        public double DoubleProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.FloatPropertyName)]
        public float FloatProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.IntPropertyName)]
        public int IntProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ShortPropertyName)]
        public short ShortProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.StringPropertyName)]
        public string StringProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ObjectPropertyName)]
        public object ObjectProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ArrayPropertyName)]
        public IList ArrayProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.MapPropertyName)]
        public IDictionary MapProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.DateTimePropertyName)]
        public DateTimeOffset DateTimeProperty { get; set; }
    }

    internal class ComponentProperties : RootLevelProperties
    {
        [JsonProperty(ConventionBasedConstants.ComponentIdentifierKey)]
        public string ComponentIdentifier { get; } = ConventionBasedConstants.ComponentIdentifierValue;
    }

    internal class ComponentLevelProperties
    {
        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentName)]
        public ComponentProperties Properties { get; set; }
    }
}
