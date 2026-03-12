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
    public class ClientTwinExtraProperties
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        [JsonIgnore]
        internal int Count => Properties.Count;

        internal dynamic this[string propertyName]
        {
            get 
            {
                if (TryGetMemberInternal(propertyName, out dynamic value))
                {
                    if (value is JsonElement jsonElementValue)
                    {
                        switch (jsonElementValue.ValueKind)
                        {
                            case JsonValueKind.String:
                                return jsonElementValue.GetString();
                            case JsonValueKind.True:
                                return true;
                            case JsonValueKind.False:
                                return false;
                            case JsonValueKind.Number:
                                return jsonElementValue.GetInt64();
                            case JsonValueKind.Array:
                            case JsonValueKind.Object:
                            case JsonValueKind.Undefined:
                            case JsonValueKind.Null:
                                return jsonElementValue; // no casting/conversion needed
                        }
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

        internal bool Contains(string propertyName)
        {
            return Properties.ContainsKey(propertyName);
        }

        internal IEnumerator GetEnumerator() => Properties.GetEnumerator();

        internal string GetPropertiesAsJson()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        internal bool TryGetValue<T>(string propertyName, out T propertyValue)
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
