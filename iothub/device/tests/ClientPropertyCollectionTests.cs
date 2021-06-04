// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientPropertyCollectionTests
    {
        private const string BoolPropertyName = "boolPropertyName";
        private const string DoublePropertyName = "doublePropertyName";
        private const string FloatPropertyName = "floatPropertyName";
        private const string IntPropertyName = "intPropertyName";
        private const string ShortPropertyName = "shortPropertyName";
        private const string StringPropertyName = "stringPropertyName";
        private const string ObjectPropertyName = "objectPropertyName";
        private const string ArrayPropertyName = "arrayPropertyName";
        private const string MapPropertyName = "mapPropertyName";
        private const string DateTimePropertyName = "dateTimePropertyName";

        private const bool BoolPropertyValue = false;
        private const double DoublePropertyValue = 1.001;
        private const float FloatPropertyValue = 1.2f;
        private const int IntPropertyValue = 12345678;
        private const short ShortPropertyValue = 1234;
        private const string StringPropertyValue = "propertyValue";

        private const string ComponentName = "testableComponent";
        private const string WritablePropertyDescription = "testableWritablePropertyDescription";
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

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectsAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { StringPropertyName, StringPropertyValue },
                { BoolPropertyName, BoolPropertyValue },
                { DoublePropertyName, DoublePropertyValue },
                { FloatPropertyName, FloatPropertyValue },
                { IntPropertyName, IntPropertyValue },
                { ShortPropertyName, ShortPropertyValue },
                { ObjectPropertyName, s_objectPropertyValue },
                { ArrayPropertyName, s_arrayPropertyValue },
                { MapPropertyName, s_mapPropertyValue },
                { DateTimePropertyName, s_dateTimePropertyValue }
            };

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

            clientProperties.TryGetValue(ArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);
            arrayOutValue.Should().BeEquivalentTo(s_arrayPropertyValue);

            clientProperties.TryGetValue(MapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);
            mapOutValue.Should().BeEquivalentTo(s_mapPropertyValue);

            clientProperties.TryGetValue(DateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { StringPropertyName, StringPropertyValue }
            };

            Action act = () => clientProperties.AddRootProperty(StringPropertyName, StringPropertyValue);
            act.Should().Throw<ArgumentException>("\"Add\" method does not support adding a key that already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { StringPropertyName, StringPropertyValue }
            };
            clientProperties.TryGetValue(StringPropertyName, out string outValue);
            outValue.Should().Be(StringPropertyValue);

            clientProperties.AddOrUpdateRootProperty(StringPropertyName, UpdatedPropertyValue);
            clientProperties.TryGetValue(StringPropertyName, out string outValueChanged);
            outValueChanged.Should().Be(UpdatedPropertyValue, "\"AddOrUpdate\" should overwrite the value if the key already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddNullPropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddRootProperty(StringPropertyName, StringPropertyValue);
            clientProperties.AddRootProperty(IntPropertyName, null);

            clientProperties.TryGetValue(StringPropertyName, out string outStringValue);
            outStringValue.Should().Be(StringPropertyValue);

            bool nullPropertyPresent = clientProperties.TryGetValue(IntPropertyName, out int? outIntValue);
            nullPropertyPresent.Should().BeTrue();
            outIntValue.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddMultiplePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddRootProperty(StringPropertyName, StringPropertyValue);
            clientProperties.AddRootProperty(IntPropertyName, IntPropertyValue);

            clientProperties.TryGetValue(StringPropertyName, out string outStringValue);
            outStringValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(IntPropertyName, out int outIntValue);
            outIntValue.Should().Be(IntPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection
            {
                { ComponentName, new Dictionary<string, object> {
                    { StringPropertyName, StringPropertyValue },
                    { BoolPropertyName, BoolPropertyValue },
                    { DoublePropertyName, DoublePropertyValue },
                    { FloatPropertyName, FloatPropertyValue },
                    { IntPropertyName, IntPropertyValue },
                    { ShortPropertyName, ShortPropertyValue },
                    { ObjectPropertyName, s_objectPropertyValue },
                    { ArrayPropertyName, s_arrayPropertyValue },
                    { MapPropertyName, s_mapPropertyValue },
                    { DateTimePropertyName, s_dateTimePropertyValue } }
                }
            };

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

            clientProperties.TryGetValue(ComponentName, ArrayPropertyName, out List<object> arrayOutValue);
            arrayOutValue.Should().HaveSameCount(s_arrayPropertyValue);
            arrayOutValue.Should().BeEquivalentTo(s_arrayPropertyValue);

            clientProperties.TryGetValue(ComponentName, MapPropertyName, out Dictionary<string, object> mapOutValue);
            mapOutValue.Should().HaveSameCount(s_mapPropertyValue);
            mapOutValue.Should().BeEquivalentTo(s_mapPropertyValue);

            clientProperties.TryGetValue(ComponentName, DateTimePropertyName, out DateTimeOffset dateTimeOutValue);
            dateTimeOutValue.Should().Be(s_dateTimePropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_AddSimpleObjectWithComponentAgainThrowsException()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);

            Action act = () => clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);
            act.Should().Throw<ArgumentException>("\"Add\" method does not support adding a key that already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanUpdateSimpleObjectWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outValue);
            outValue.Should().Be(StringPropertyValue);

            clientProperties.AddOrUpdateComponentProperty(ComponentName, StringPropertyName, UpdatedPropertyValue);
            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outValueChanged);
            outValueChanged.Should().Be(UpdatedPropertyValue, "\"AddOrUpdate\" should overwrite the value if the key already exists in the collection.");
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddNullPropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);
            clientProperties.AddComponentProperty(ComponentName, IntPropertyName, null);

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outStringValue);
            outStringValue.Should().Be(StringPropertyValue);

            bool nullPropertyPresent = clientProperties.TryGetValue(ComponentName, IntPropertyName, out int? outIntValue);
            nullPropertyPresent.Should().BeTrue();
            outIntValue.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddMultiplePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);
            clientProperties.AddComponentProperty(ComponentName, IntPropertyName, IntPropertyValue);

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outStringValue);
            outStringValue.Should().Be(StringPropertyValue);

            clientProperties.TryGetValue(ComponentName, IntPropertyName, out int outIntValue);
            outIntValue.Should().Be(IntPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddSimpleWritablePropertyAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();

            var writableResponse = new NewtonsoftJsonWritablePropertyResponse(StringPropertyValue, StatusCodes.OK, 2, WritablePropertyDescription);
            clientProperties.AddRootProperty(StringPropertyName, writableResponse);

            clientProperties.TryGetValue(StringPropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);
            outValue.Value.Should().Be(writableResponse.Value);
            outValue.AckCode.Should().Be(writableResponse.AckCode);
            outValue.AckVersion.Should().Be(writableResponse.AckVersion);
            outValue.AckDescription.Should().Be(writableResponse.AckDescription);
        }

        [TestMethod]
        public void ClientPropertyCollection_CanAddWritablePropertyWithComponentAndGetBackWithoutDeviceClient()
        {
            var clientProperties = new ClientPropertyCollection();

            var writableResponse = new NewtonsoftJsonWritablePropertyResponse(StringPropertyValue, StatusCodes.OK, 2, WritablePropertyDescription);
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, writableResponse);

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out NewtonsoftJsonWritablePropertyResponse outValue);
            outValue.Value.Should().Be(writableResponse.Value);
            outValue.AckCode.Should().Be(writableResponse.AckCode);
            outValue.AckVersion.Should().Be(writableResponse.AckVersion);
            outValue.AckDescription.Should().Be(writableResponse.AckDescription);
        }

        [TestMethod]
        public void ClientPropertyCollection_AddingComponentAddsComponentIdentifier()
        {
            var clientProperties = new ClientPropertyCollection();
            clientProperties.AddComponentProperty(ComponentName, StringPropertyName, StringPropertyValue);

            clientProperties.TryGetValue(ComponentName, StringPropertyName, out string outValue);
            clientProperties.TryGetValue(ComponentName, ConventionBasedConstants.ComponentIdentifierKey, out string componentOut);

            outValue.Should().Be(StringPropertyValue);
            componentOut.Should().Be(ConventionBasedConstants.ComponentIdentifierValue);
        }
    }

    internal class CustomClientProperty
    {
        // The properties in here need to be public otherwise NewtonSoft.Json cannot serialize and deserialize them properly.
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
