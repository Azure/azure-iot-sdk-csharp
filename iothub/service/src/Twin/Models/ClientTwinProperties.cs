// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        [JsonProperty("$version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? Version { get; internal set; }

        /// <summary>
        /// Metadata about the properties.
        /// </summary>
        [JsonProperty("$metadata")]
        public ClientTwinMetadata Metadata { get; set; } = new();

        [JsonExtensionData]
        internal IDictionary<string, JToken> Properties { get; set; } = new Dictionary<string, JToken>();

        /// <summary>
        /// Gets the count of properties in the collection.
        /// </summary>
        [JsonIgnore]
        public int Count => Properties.Count;

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Value for the given property name.</returns>
        /// <exception cref="InvalidOperationException">When the specified <paramref name="propertyName"/> does not exist in the collection.</exception>
        public dynamic this[string propertyName]
        {
            get => TryGetMemberInternal(propertyName, out dynamic value)
                ? value
                : throw new InvalidOperationException($"Unexpected property name '{propertyName}'.");
            set => Properties[propertyName] = value is null || value is JToken
                ? value
                : JToken.FromObject(value);
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

        /// <summary>
        /// Gets the specified property by name as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The resulting value.</param>
        /// <returns>True if the property exists and could be converted, otherwise false.</returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out JToken jTokenValue))
            {
                return false;
            }

            // Try cast.
            try
            {
                propertyValue = jTokenValue.Value<T>();
                return true;
            }
            catch (InvalidCastException)
            { }

            // Try convert.
            try
            {
                propertyValue = jTokenValue.ToObject<T>();
                return true;
            }
            catch (InvalidCastException)
            { }

            return false;
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => Properties.GetEnumerator();

        /// <summary>
        /// The property payload as a JSON string.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(Properties);
        }

        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            result = default;

            if (!Properties.TryGetValue(propertyName, out JToken jTokenValue))
            {
                return false;
            }

            result = jTokenValue;

            return true;
        }
    }
}
