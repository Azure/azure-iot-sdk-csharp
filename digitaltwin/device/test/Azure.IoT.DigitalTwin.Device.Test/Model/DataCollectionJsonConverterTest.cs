// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Azure.Iot.DigitalTwin.Device.Model;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Azure.IoT.DigitalTwin.Device.Test.Model
{
    public class DataCollectionJsonConverterTest
    {
        [Fact]
        public void TestToObjectWithNullValue()
        {
            var converter = new DataCollectionJsonConverter();
            var writer = Substitute.For<JsonWriter>();
            var serializer = new JsonSerializer();

            converter.WriteJson(writer, null, serializer);

            writer.Received().WriteNull();
        }

        [Fact]
        public void TestToObjectWithNonDataCollectionValue()
        {
            var converter = new DataCollectionJsonConverter();
            var writer = Substitute.For<JsonWriter>();
            var serializer = new JsonSerializer();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => converter.WriteJson(writer, string.Empty, serializer));

            Assert.Equal("Object passed is not of type DataCollection.", ex.Message);
        }

        [Fact]
        public void TestCanConvertWithNonDataCollectionType()
        {
            var converter = new DataCollectionJsonConverter();

            Assert.False(converter.CanConvert(typeof(DigitalTwinCommandRequest)));
        }

        [Fact]
        public void TestCanConvertWithDataCollectionType()
        {
            var converter = new DataCollectionJsonConverter();

            Assert.True(converter.CanConvert(typeof(DataCollection)));
        }

        [Fact]
        public void TestCanConvertWithInheritedDataCollectionType()
        {
            var converter = new DataCollectionJsonConverter();

            Assert.True(converter.CanConvert(typeof(TestDataCollection)));
        }

        internal class TestDataCollection : DataCollection
        {
        }
    }
}
