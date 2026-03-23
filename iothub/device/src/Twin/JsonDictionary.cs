// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public class JsonDictionary : IDictionary<string, object>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        /// <summary>
        /// 
        /// </summary>
        public JsonDictionary()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        public JsonDictionary(Dictionary<string, object> properties)
        {
            foreach (var key in properties.Keys)
            {
                Properties.Add(key, JsonSerializer.SerializeToElement(properties[key], JsonSerializerSettings.Options));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JsonElement> Properties { get; set; } = new Dictionary<string, JsonElement>();

        /// <inheritdoc/>
        public ICollection<string> Keys => Properties.Keys;

        /// <inheritdoc/>
        public ICollection<object> Values => (ICollection<object>) Properties.Values;

        /// <inheritdoc/>
        public int Count => Properties.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => Properties.IsReadOnly;

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
                    if (value is JsonDictionary jsonDictionary)
                    {
                        return jsonDictionary;
                    }
                    else if (value is JsonElement jsonElementValue)
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
            set 
            {
                if (value is null)
                {
                    // This creates a JsonElement with ValueType "null"
                    Properties[propertyName] = JsonDocument.Parse("null").RootElement;
                }
                else if (value is JsonElement jsonElement)
                {
                    Properties[propertyName] = jsonElement;
                }
                else
                {
                    Properties[propertyName] = JsonSerializer.SerializeToElement(value, JsonSerializerSettings.Options);
                }
            } 
        }

        internal static object FromJsonElement(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    string s = jsonElement.GetString();

                    // String values may be a date time, so check before returning them
                    // as strings.
                    if (DateTimeOffset.TryParse(s, out DateTimeOffset dateTimeOffset))
                    {
                        return dateTimeOffset;
                    }

                    return s;
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt32(out int integerValue))
                    {
                        return integerValue;
                    }
                    else if (jsonElement.TryGetInt64(out long longValue))
                    {
                        return longValue;
                    }
                    else if (jsonElement.TryGetDouble(out double doubleValue))
                    {
                        return doubleValue;
                    }
                    throw new FormatException("Could not convert JsonElement number to integer, long, or double");
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
        /// <returns></returns>
        public string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetValue(string propertyName, out dynamic propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out JsonElement jsonElement))
            {
                return false;
            }

            propertyValue = FromJsonElement(propertyValue);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        public bool TryGetAndDeserializeValue<T>(string propertyName, out T propertyValue)
        {
            propertyValue = default;

            if (!Properties.TryGetValue(propertyName, out JsonElement jsonElement))
            {
                return false;
            }

            try
            {
                propertyValue = JsonSerializer.Deserialize<T>(jsonElement, JsonSerializerSettings.Options);
                return true;
            }
            catch (InvalidCastException)
            { }

            return false;
        }

        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            result = default;

            if (!Properties.TryGetValue(propertyName, out JsonElement jsonElement))
            {
                return false;
            }

            result = jsonElement;

            return true;
        }

        /// <inheritdoc/>
        public void Add(string key, object value)
        {
            Properties.Add(key, JsonSerializer.SerializeToElement(value, JsonSerializerSettings.Options));
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool Remove(string key)
        {
            return Properties.Remove(key);
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<string, object> item)
        {
            Properties.Add(item.Key, JsonSerializer.SerializeToElement(item.Value, JsonSerializerSettings.Options));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            Properties.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<string, object> item)
        {
            if (Properties.TryGetValue(item.Key, out JsonElement value))
            {
                if (FromJsonElement(value).Equals(item.Value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<string, JsonElement> src in Properties)
            {
                array[arrayIndex++] = new KeyValuePair<string, object>(src.Key, src.Value);
            }
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<string, object> item)
        {
            KeyValuePair<string, JsonElement> converted = new(item.Key, JsonSerializer.SerializeToElement(item, JsonSerializerSettings.Options));
            return Properties.Remove(converted);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var key in Properties.Keys)
            {
                yield return new KeyValuePair<string, object>(key, FromJsonElement(Properties[key]));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal byte[] GetObjectBytes()
        {
            return JsonSerializer.SerializeToUtf8Bytes(Properties, JsonSerializerSettings.Options);
        }
    }
}
