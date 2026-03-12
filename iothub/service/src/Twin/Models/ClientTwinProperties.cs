// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Devices.Utilities;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Gets and sets the twin desired properties.
    /// </summary>
    public class ClientTwinProperties
    {
        /// <summary>
        /// Gets the version of the collection.
        /// </summary>
        [JsonPropertyName("$version")]
        public long? Version { get; internal set; }

        /// <summary>
        /// Metadata about the properties.
        /// </summary>
        [JsonPropertyName("$metadata")]
        public ClientTwinMetadata Metadata { get; set; } = new();

        /// <summary>
        /// All of the user-defined properties for this twin document
        /// </summary>
        [JsonPropertyName("properties")]
        public ClientTwinExtraProperties Properties { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public dynamic this[string propertyName]
        {
            get => Properties[propertyName];
            set => Properties[propertyName] = value;
        }

        /// <summary>
        /// Gets the count of properties in the collection.
        /// </summary>
        [JsonIgnore]
        public int Count => Properties.Count;

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns>true if the specified property is present; otherwise, false.</returns>
        public bool Contains(string propertyName)
        {
            return Properties.Contains(propertyName);
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => Properties.GetEnumerator();

        /// <summary>
        /// The property payload as a JSON string.
        /// </summary>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// Gets the specified property by name as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The resulting value.</param>
        /// <returns>True if the property exists and could be converted, otherwise false.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            return Properties.TryGetValue(propertyName, out propertyValue);
        }

    }
}
