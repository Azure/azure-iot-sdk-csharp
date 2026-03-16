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
    public class ClientTwinProperties
    {
        internal const string MetadataName = "$metadata";
        internal const string LastUpdatedName = "$lastUpdated";
        internal const string LastUpdatedVersionName = "$lastUpdatedVersion";
        internal const string VersionName = "$version";
        private readonly JsonDocument _metadata;

        /// <summary>
        /// 
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the version of the twin collection.
        /// </summary>
        public long Version 
        { 
            get 
            {
                if (_metadata.RootElement.TryGetProperty(VersionName, out JsonElement versionJsonElement))
                {
                    if (versionJsonElement.TryGetInt64(out long version))
                    {
                        return version;
                    }
                }

                return default;
            }
        }


        /// <summary>
        /// Gets the Metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public InitialTwinMetadata GetMetadata()
        {
            return new InitialTwinMetadata(GetLastUpdatedOnUtc, GetLastUpdatedVersion);
        }

        /// <summary>
        /// Gets the time when this property was last updated in UTC.
        /// </summary>
        public DateTimeOffset GetLastUpdatedOnUtc
        {
            get
            {
                if (_metadata.RootElement.TryGetProperty(LastUpdatedName, out JsonElement lastUpdatedJsonElement))
                {
                    if (lastUpdatedJsonElement.TryGetDateTimeOffset(out DateTimeOffset lastUpdated))
                    {
                        return lastUpdated;
                    }
                }

                return default;
            }
        }

        /// <summary>
        /// Gets the LastUpdatedVersion for this property.
        /// </summary>
        /// <returns>LastUpdatdVersion if present, null otherwise.</returns>
        public long? GetLastUpdatedVersion
        {
            get
            {
                if (_metadata.RootElement.TryGetProperty(LastUpdatedVersionName, out JsonElement lastUpdatedVersionJsonElement))
                {
                    if (lastUpdatedVersionJsonElement.TryGetInt64(out long lastUpdatedVersion))
                    {
                        return lastUpdatedVersion;
                    }
                }

                return default;
            }
        }

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
