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
    public class ClientPropertyCollectionTestsNewtonsoft
    {
        internal const string RootBoolPropertyName = "rootBoolPropertyName";
        internal const string RootDoublePropertyName = "rootDoublePropertyName";
        internal const string RootFloatPropertyName = "rootFloatPropertyName";
        internal const string RootIntPropertyName = "rootIntPropertyName";
        internal const string RootShortPropertyName = "rootShortPropertyName";
        internal const string RootStringPropertyName = "rootStringPropertyName";
        internal const string RootObjectPropertyName = "rootObjectPropertyName";
        internal const string RootArrayPropertyName = "rootArrayPropertyName";
        internal const string RootMapPropertyName = "rootMapPropertyName";
        internal const string RootDateTimePropertyName = "rootDateTimePropertyName";
        internal const string RootWritablePropertyName = "rootWritablePropertyName";

        internal const string ComponentName = "testableComponent";
        internal const string ComponentBoolPropertyName = "componentBoolPropertyName";
        internal const string ComponentDoublePropertyName = "componentDoublePropertyName";
        internal const string ComponentFloatPropertyName = "componentFloatPropertyName";
        internal const string ComponentIntPropertyName = "componentIntPropertyName";
        internal const string ComponentShortPropertyName = "componentShortPropertyName";
        internal const string ComponentStringPropertyName = "componentStringPropertyName";
        internal const string ComponentObjectPropertyName = "componentObjectPropertyName";
        internal const string ComponentArrayPropertyName = "componentArrayPropertyName";
        internal const string ComponentMapPropertyName = "componentMapPropertyName";
        internal const string ComponentDateTimePropertyName = "componentDateTimePropertyName";
        internal const string ComponentWritablePropertyName = "componentWritablePropertyName";

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

        // Create a writable property response with the expected values.
        private static readonly IWritablePropertyResponse s_writablePropertyResponse = new NewtonsoftJsonWritablePropertyResponse(
            propertyValue: StringPropertyValue,
            ackCode: CommonClientResponseCodes.OK,
            ackVersion: 2,
            ackDescription: "testableWritablePropertyDescription");

        // Create an object that represents a client instance having top-level and component-level properties.
        private static readonly TestProperties s_testClientProperties = new TestProperties
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
            DateTimeProperty = s_dateTimePropertyValue,
            WritablePropertyResponse = s_writablePropertyResponse,
            ComponentProperties = new ComponentProperties
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
                DateTimeProperty = s_dateTimePropertyValue,
                WritablePropertyResponse = s_writablePropertyResponse,
            }
        };

        private static string getClientPropertiesStringResponse = JsonConvert.SerializeObject(new Dictionary<string, object> { { "reported", s_testClientProperties } });

        private static ClientPropertiesAsDictionary clientPropertiesAsDictionary = JsonConvert.DeserializeObject<ClientPropertiesAsDictionary>(getClientPropertiesStringResponse);

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetValue()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act, assert

            clientProperties.TryGetValue(RootStringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(RootBoolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(BoolPropertyValue);

            clientProperties.TryGetValue(RootDoublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(DoublePropertyValue);

            clientProperties.TryGetValue(RootFloatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(FloatPropertyValue);

            clientProperties.TryGetValue(RootIntPropertyName, out int intOutValue);
            intOutValue.Should().Be(IntPropertyValue);

            clientProperties.TryGetValue(RootShortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(ShortPropertyValue);

            clientProperties.TryGetValue(RootObjectPropertyName, out CustomClientProperty objectOutValue);
            objectOutValue.Id.Should().Be(s_objectPropertyValue.Id);
            objectOutValue.Name.Should().Be(s_objectPropertyValue.Name);

            // The two lists won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the list is deserialized to a JObject.
            clientProperties.TryGetValue(RootArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);

            // The two dictionaries won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the dictionary is deserialized to a JObject.
            clientProperties.TryGetValue(RootMapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);

            clientProperties.TryGetValue(RootDateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanGetValueWithComponent()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act, assert

            clientProperties.TryGetValue(ComponentName, ComponentStringPropertyName, out string stringOutValue);
            stringOutValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentBoolPropertyName, out bool boolOutValue);
            boolOutValue.Should().Be(BoolPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentDoublePropertyName, out double doubleOutValue);
            doubleOutValue.Should().Be(DoublePropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentFloatPropertyName, out float floatOutValue);
            floatOutValue.Should().Be(FloatPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentIntPropertyName, out int intOutValue);
            intOutValue.Should().Be(IntPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentShortPropertyName, out short shortOutValue);
            shortOutValue.Should().Be(ShortPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentObjectPropertyName, out CustomClientProperty objectOutValue);
            objectOutValue.Id.Should().Be(s_objectPropertyValue.Id);
            objectOutValue.Name.Should().Be(s_objectPropertyValue.Name);

            // The two lists won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the list is deserialized to a JObject.
            clientProperties.TryGetValue(ComponentName, ComponentArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);

            // The two dictionaries won't be exactly equal since TryGetValue doesn't implement nested deserialization
            // => the complex object inside the dictionary is deserialized to a JObject.
            clientProperties.TryGetValue(ComponentName, ComponentMapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);

            clientProperties.TryGetValue(ComponentName, ComponentDateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonsoft_CanAddSimpleWritablePropertyAndGetBack()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(RootWritablePropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);

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
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(ComponentName, ComponentWritablePropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);

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
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            clientProperties.TryGetValue(ComponentName, ComponentStringPropertyName, out string outValue);
            clientProperties.TryGetValue(ComponentName, ConventionBasedConstants.ComponentIdentifierKey, out string componentOut);

            // assert
            outValue.Should().Be(StringPropertyValue);
            componentOut.Should().Be(ConventionBasedConstants.ComponentIdentifierValue);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueShouldReturnFalseIfValueNotFound()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

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
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(ComponentName, "thisPropertyDoesNotExist", out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(RootStringPropertyName, out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueWithComponentShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(ComponentName, ComponentStringPropertyName, out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollectionNewtonSoft_TryGetValueWithComponentShouldReturnFalseIfNotAComponent()
        {
            // arrange
            var clientProperties = ClientPropertyCollection.FromClientPropertiesAsDictionary(clientPropertiesAsDictionary.Reported, DefaultPayloadConvention.Instance);
            string incorrectlyMappedComponentName = ComponentMapPropertyName;
            string incorrectlyMappedComponentPropertyName = "key1";

            // act
            bool isValueRetrieved = clientProperties.TryGetValue(incorrectlyMappedComponentName, incorrectlyMappedComponentPropertyName, out object propertyValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            propertyValue.Should().Be(default);
        }
    }

    internal class TestProperties
    {
        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootBoolPropertyName)]
        public bool BooleanProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootDoublePropertyName)]
        public double DoubleProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootFloatPropertyName)]
        public float FloatProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootIntPropertyName)]
        public int IntProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootShortPropertyName)]
        public short ShortProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootStringPropertyName)]
        public string StringProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootObjectPropertyName)]
        public object ObjectProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootArrayPropertyName)]
        public IList ArrayProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootMapPropertyName)]
        public IDictionary MapProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootDateTimePropertyName)]
        public DateTimeOffset DateTimeProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.RootWritablePropertyName)]
        public IWritablePropertyResponse WritablePropertyResponse { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentName)]
        public ComponentProperties ComponentProperties { get; set; }
    }

    internal class ComponentProperties
    {
        [JsonProperty(ConventionBasedConstants.ComponentIdentifierKey)]
        public string ComponentIdentifier { get; } = ConventionBasedConstants.ComponentIdentifierValue;

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentBoolPropertyName)]
        public bool BooleanProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentDoublePropertyName)]
        public double DoubleProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentFloatPropertyName)]
        public float FloatProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentIntPropertyName)]
        public int IntProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentShortPropertyName)]
        public short ShortProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentStringPropertyName)]
        public string StringProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentObjectPropertyName)]
        public object ObjectProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentArrayPropertyName)]
        public IList ArrayProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentMapPropertyName)]
        public IDictionary MapProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentDateTimePropertyName)]
        public DateTimeOffset DateTimeProperty { get; set; }

        [JsonProperty(ClientPropertyCollectionTestsNewtonsoft.ComponentWritablePropertyName)]
        public IWritablePropertyResponse WritablePropertyResponse { get; set; }
    }
}
