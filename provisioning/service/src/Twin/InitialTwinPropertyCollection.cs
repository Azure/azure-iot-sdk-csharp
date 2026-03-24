// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents a collection of properties for device twin.
    /// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class InitialTwinPropertyCollection
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        /// <summary>
        /// Gets the version of the collection.
        /// </summary>
        [JsonPropertyName("$version")]
        public long? Version { get; set; }

        /// <summary>
        /// Metadata about the properties.
        /// </summary>
        [JsonPropertyName("$metadata")]
        public InitialTwinMetadata Metadata { get; set; }

        /// <summary>
        /// The properties in this dictionary
        /// </summary>
        [JsonExtensionData]
        public JsonDictionary Properties { get; set; } = new();

        /// <summary>
        /// The number of properties in this dictionary
        /// </summary>
        [JsonIgnore]
        public int Count => Properties.Count;

        /// <summary>
        /// The custom getter/setter for this dictionary
        /// </summary>
        /// <param name="propertyName">The property name to get/set</param>
        /// <returns>The property value cast as the appropriate type</returns>
        public dynamic this[string propertyName]
        {
            get => Properties[propertyName];
            set => Properties[propertyName] = value;
        }

        /// <summary>
        /// Returns true if this dictionary contains a property with the provided name.
        /// </summary>
        /// <param name="propertyName">The name of the property to check for</param>
        /// <returns>True if this dictionary contains a property with the provided name.</returns>
        public bool ContainsKey(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Try to retrieve and cast a property with the given name into the given type.
        /// </summary>
        /// <param name="propertyName">The name of the property to try to retrieve.</param>
        /// <param name="propertyValue">The value of the property cast as the given type, if it was present and could be cast.</param>
        /// <returns>True if the property was present and could be cast as the given type.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            return Properties.TryGetValue<T>(propertyName, out propertyValue);
        }

        /// <summary>
        /// Get the enumerator for this dictionary
        /// </summary>
        /// <returns>The enumerator for this dictionary</returns>
        public IEnumerator GetEnumerator() => Properties.GetEnumerator();

        /// <summary>
        /// Serialize this dictionary into a Json string.
        /// </summary>
        /// <returns>This dictionary as a Json string</returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// Try to retrieve and deserialize a property with the given name into the given type.
        /// </summary>
        /// <param name="propertyName">The name of the property to try to retrieve.</param>
        /// <param name="propertyValue">The value of the property deserialized as the given type, if it was present and could be deserialized.</param>
        /// <returns>True if the property was present and could be deserialized as the given type.</returns>
        public bool TryGetAndDeserializeValue<T>(string propertyName, out T propertyValue)
        {
            return Properties.TryGetAndDeserializeValue<T>(propertyName, out propertyValue);
        }
    }
}
