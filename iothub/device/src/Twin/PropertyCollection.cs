// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of twin properties.
    /// </summary>
    public abstract class PropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The version of the client twin properties.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the client twin properties.</value>
        [JsonPropertyName("$version")]
        [JsonInclude]
        public long Version { get; protected internal set; }

        [JsonExtensionData]
        [JsonInclude]
        internal IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The count of properties in the collection.
        /// </summary>
        public int Count => Properties.Count;

        /// <summary>
        /// The payload convention that defines a specific serializer as well as a specific content encoding for the payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Gets the value associated with the <paramref name="propertyName"/> in the reported property collection.
        /// </summary>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="propertyName">The key of the property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the object from the collection.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns>True if a value of type <c>T</c> with the specified key was found; otherwise, it returns false.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out object theValue))
            {
                return false;
            }

            if (theValue is T theValueAsT)
            {
                propertyValue = theValueAsT;
                return true;
            }

            if (theValue is JsonElement jsonElementValue)
            {
                try
                {
                    propertyValue = PayloadConvention
                        .PayloadSerializer
                        .DeserializeToType<T>(jsonElementValue.GetRawText());
                    return true;
                }
                catch (JsonException)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// The client twin properties, as a serialized string.
        /// </summary>
        public string GetSerializedString()
        {
            return PayloadConvention.PayloadSerializer.SerializeToString(Properties);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in Properties)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal byte[] GetObjectBytes()
        {
            return PayloadConvention.GetObjectBytes(Properties);
        }
    }
}
