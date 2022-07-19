// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class WritableClientPropertyCollectionTests
    {
        private const string VersionName = "$version";

        private const string IntPropertyName = "intPropertyName";
        private const string StringPropertyName = "stringPropertyName";
        private const string ObjectPropertyName = "objectPropertyName";
        private const string MapPropertyName = "mapPropertyName";

        private const int IntPropertyValue = 12345678;
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
        public void ClientPropertyCollection_TryGetWritableClientPropertyShouldReturnTrueIfPropertyFound()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { StringPropertyName, StringPropertyValue },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(StringPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeTrue();
            writableClientProperty.TryGetValue(out string outStringValue).Should().BeTrue();
            outStringValue.Should().Be(StringPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyWithComponentShouldReturnTrueIfPropertyFound()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { ComponentName, new Dictionary<string, object>
                    {
                        { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                        { StringPropertyName, StringPropertyValue },
                    }
                },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(ComponentName, StringPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeTrue();
            writableClientProperty.TryGetValue(out string outStringValue).Should().BeTrue();
            outStringValue.Should().Be(StringPropertyValue);
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyShouldReturnFalseIfPropertyNotFound()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { StringPropertyName, StringPropertyValue },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(IntPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeFalse();
            writableClientProperty.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyWithComponentShouldReturnFalseIfPropertyNotFound()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { ComponentName, new Dictionary<string, object>
                    {
                        { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                        { ObjectPropertyName, s_objectPropertyValue }
                    }
                },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(ComponentName, IntPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeFalse();
            writableClientProperty.Should().BeNull();

            writableClientProperty.TryGetValue(out object value).Should().BeTrue();
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetValueShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { StringPropertyName, StringPropertyValue },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(StringPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeTrue();

            // act
            bool isValueRetrieved = writableClientProperty.TryGetValue(out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetValueWithComponentShouldReturnFalseIfValueCouldNotBeDeserialized()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { ComponentName, new Dictionary<string, object>
                    {
                        { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                        { ObjectPropertyName, s_objectPropertyValue }
                    }
                },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(ComponentName, ObjectPropertyName, out WritableClientProperty writableClientProperty);

            // assert
            isWritablePropertyRetrieved.Should().BeTrue();

            // act
            bool isValueRetrieved = writableClientProperty.TryGetValue(out int outIntValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outIntValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyWithComponentShouldReturnFalseIfNotAComponent()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { StringPropertyName, StringPropertyValue },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            string incorrectlyMappedComponentName = MapPropertyName;
            string incorrectlyMappedComponentPropertyName = "key1";


            // act
            bool isWritablePropertyRetrieved = writableClientProperties.TryGetWritableClientProperty(
                incorrectlyMappedComponentName,
                incorrectlyMappedComponentPropertyName,
                out WritableClientProperty propertyValue);

            // assert
            isWritablePropertyRetrieved.Should().BeFalse();
            propertyValue.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyWithNullPropertyNameReturnsFalse()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { IntPropertyName, IntPropertyValue },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isPresent = writableClientProperties.TryGetWritableClientProperty(null, out WritableClientProperty value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritbleClientPropertyWithNullComponentNameReturnsFalse()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { ComponentName, new Dictionary<string, object>
                    {
                        { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                        { IntPropertyName, IntPropertyValue },
                    }
                },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isPresent = writableClientProperties.TryGetWritableClientProperty(null, IntPropertyName, out WritableClientProperty value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetWritableClientPropertyWithComponentAndNullPropertyNameReturnsFalse()
        {
            // arrange
            var props = new Dictionary<string, object>
            {
                { ComponentName, new Dictionary<string, object>
                    {
                        { ConventionBasedConstants.ComponentIdentifierKey, ConventionBasedConstants.ComponentIdentifierValue },
                        { IntPropertyName, IntPropertyValue },
                    }
                },
                { VersionName, 2 },
            };
            var writableRequest = ConvertToServiceUpdateRequestedDictionary(props);
            var writableClientProperties = new WritableClientPropertyCollection(writableRequest, DefaultPayloadConvention.Instance);

            // act
            bool isPresent = writableClientProperties.TryGetWritableClientProperty(ComponentName, null, out WritableClientProperty value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().BeNull();
        }

        // The service update requested properties are always deserialized into a dictionary using Newtonsoft.Json.
        // So, even though we have a dictionary object here for testing, we'll need to serialize it and deserialize it back using Newtonsoft.Json.
        private IDictionary<string, object> ConvertToServiceUpdateRequestedDictionary(IDictionary<string, object> input)
        {
            string serializedServiceUpdateRequestedPropertiesDictionary = JsonConvert.SerializeObject(input);
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedServiceUpdateRequestedPropertiesDictionary);
        }
    }
}
