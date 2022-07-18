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
        public void ClientPropertyCollection_TryGetValueShouldReturnTrueIfPropertyFound()
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(StringPropertyName, out string outStringValue);

            // assert
            isValueRetrieved.Should().BeTrue();
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
        public void ClientPropertyCollection_TryGetValueWithComponentShouldReturnTrueIfPropertyFound()
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(ComponentName, StringPropertyName, out string outStringValue);

            // assert
            isValueRetrieved.Should().BeTrue();
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
        public void ClientPropertyCollection_TryGetValueShouldReturnFalseIfPropertyNotFound()
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(IntPropertyName, out object outStringValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outStringValue.Should().Be(default);
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
        }

        [TestMethod]
        public void ClientPropertyCollection_TryGetValueWithComponentShouldReturnFalseIfPropertyNotFound()
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(ComponentName, IntPropertyName, out object outCustomValue);

            // assert
            isValueRetrieved.Should().BeFalse();
            outCustomValue.Should().Be(default);
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(StringPropertyName, out int outIntValue);

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
            bool isValueRetrieved = writableClientProperties.TryGetValue(ComponentName, ObjectPropertyName, out int outIntValue);

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
        public void ClientPropertyCollection_TryGetValueWithComponentShouldReturnFalseIfNotAComponent()
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
            bool isValueRetrieved = writableClientProperties.TryGetValue(incorrectlyMappedComponentName, incorrectlyMappedComponentPropertyName, out object propertyValue);

            isValueRetrieved.Should().BeFalse();
            propertyValue.Should().Be(default);
        }

        [TestMethod]
        public void ClientPropertyCollection_ContainsWithNullPropertyNameThrows()
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
            Action testAction = () => writableClientProperties.Contains(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ClientPropertyCollection_ContainsWithNullComponentNameThrows()
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
            Action testAction = () => writableClientProperties.Contains(null, IntPropertyName);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void ClientPropertyCollection_ContainsWithComponentAndNullPropertyNameThrows()
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
            Action testAction = () => writableClientProperties.Contains(ComponentName, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
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
        public void ClientPropertyCollection_TryGetValueWithNullPropertyNameReturnsFalse()
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
            bool isPresent = writableClientProperties.TryGetValue(null, out object value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().Be(default);
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
        public void ClientPropertyCollection_TryGetValueWithNullComponentNameReturnsFalse()
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
            bool isPresent = writableClientProperties.TryGetValue(null, IntPropertyName, out object value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().Be(default);
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

        [TestMethod]
        public void ClientPropertyCollection_TryGetValueWithComponentAndNullPropertyNameReturnsFalse()
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
            bool isPresent = writableClientProperties.TryGetValue(ComponentName, null, out object value);

            // assert
            isPresent.Should().BeFalse();
            value.Should().Be(default);
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
