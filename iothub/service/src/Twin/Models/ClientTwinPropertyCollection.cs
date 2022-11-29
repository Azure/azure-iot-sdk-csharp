// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents a collection of properties for the twin.
    /// </summary>
    public sealed class ClientTwinPropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The version of the collection of properties.
        /// </summary>
        [JsonPropertyName("$version")]
        public long Version { get; internal set; }

        /// <summary>
        /// Metadata about the collection of properties.
        /// </summary>
        [JsonPropertyName("$metadata")]
        public ClientTwinMetadata Metadata { get; set; } = new();

        [JsonExtensionData]
        internal IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// The count of properties in the collection.
        /// </summary>
        public int Count => Properties.Count;

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Value for the given property name.</returns>
        /// <exception cref="InvalidOperationException">When the specified <paramref name="propertyName"/> does not exist in the collection.</exception>
        public dynamic this[string propertyName]
        {
            get => Properties.TryGetValue(propertyName, out dynamic value)
                ? value
                : throw new InvalidOperationException($"Unexpected property name '{propertyName}'.");
            set => Properties[propertyName] = value;
        }

        /// <summary>
        /// Gets the specified property's value by name to the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="propertyValue">The retrieved value.</param>
        /// <returns>True if the property name was available and the type could be converted, otherwise false.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue) where T : class
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
                    propertyValue = JsonSerializer.Deserialize<T>(jsonElementValue.GetRawText());
                    return true;
                }
                catch (JsonException)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns>true if the specified property is present; otherwise, false.</returns>
        public bool Contains(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, object> kvp in Properties)
            {
                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            foreach (KeyValuePair<string, object> kvp in Properties)
            {
                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }
    }
}
