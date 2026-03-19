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
    /// Gets and sets the twin desired properties.
    /// </summary>
    public class ClientTwinProperties
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
        public ClientTwinMetadata Metadata { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public JsonDictionary Properties { get; set; } = new();

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public int Count => Properties.Count;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public dynamic this[string propertyName]
        {
            get
            {
                if (TryGetMemberInternal(propertyName, out dynamic value))
                {
                    if (value is JsonElement jsonElementValue)
                    {
                        return FromJsonElement(jsonElementValue);
                    }
                    return value;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected property name '{propertyName}'.");
                }
            }
            set => Properties[propertyName] = value is null || value is JsonElement
                ? value
                : JsonSerializer.SerializeToElement(value);
        }

        private static object FromJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    return jsonElement.GetInt64();
                case JsonValueKind.Array:
                    List<object> arrayWithElements = new List<object>();
                    foreach (JsonElement jsonArrayElement in jsonElement.EnumerateArray())
                    {
                        arrayWithElements.Add(FromJsonElement(jsonArrayElement));
                    }
                    return arrayWithElements;
                
                case JsonValueKind.Object:
                    JsonDictionary objectFields = new();
                    foreach (JsonProperty key in jsonElement.EnumerateObject())
                    { 
                        objectFields.TryAdd(key.Name, FromJsonElement(key.Value));
                    }
                    return objectFields;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Undefined:
                    return null;
            }

            // Should never happen
            throw new ArgumentException("Unrecognized Json element type");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool Contains(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator() => Properties.GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out object jTokenValue))
            {
                return false;
            }

            try
            {
                if (jTokenValue == null)
                {
                    return false;
                }
                else if (jTokenValue is JsonElement jsonElement)
                {
                    propertyValue = JsonSerializer.Deserialize<T>(jsonElement, JsonSerializerSettings.Options);
                    return true;
                }

                // All elements in the dictionary should be null or a Json element, but TODO to check this
            }
            catch (InvalidCastException)
            { }

            return false;
        }

        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            result = default;

            if (!Properties.TryGetValue(propertyName, out object jTokenValue))
            {
                return false;
            }

            result = jTokenValue;

            return true;
        }

    }
}
