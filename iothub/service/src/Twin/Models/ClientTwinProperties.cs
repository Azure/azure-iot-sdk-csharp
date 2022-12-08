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
    [JsonConverter(typeof(ClientTwinPropertiesJsonConverter))]
    public class ClientTwinProperties : IEnumerable
    {
        internal const string MetadataName = "$metadata";
        internal const string LastUpdatedName = "$lastUpdated";
        internal const string LastUpdatedVersionName = "$lastUpdatedVersion";
        internal const string VersionName = "$version";

        private readonly JObject _metadata;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public ClientTwinProperties()
            : this((JObject)null)
        {
        }

        /// <summary>
        /// Creates an instance of this class using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        public ClientTwinProperties(string twinJson)
            : this(JObject.Parse(twinJson))
        {
        }

        /// <summary>
        /// Creates an instance of this class using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public ClientTwinProperties(string twinJson, string metadataJson)
            : this(JObject.Parse(twinJson), JObject.Parse(metadataJson))
        {
        }

        /// <summary>
        /// Creates an instance of this class using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        internal ClientTwinProperties(JObject twinJson)
        {
            TwinData = twinJson ?? new JObject();

            if (TwinData.TryGetValue(MetadataName, out JToken metadataJToken))
            {
                _metadata = metadataJToken as JObject;
            }
        }

        /// <summary>
        /// Creates an instance of this class using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public ClientTwinProperties(JObject twinJson, JObject metadataJson)
        {
            TwinData = twinJson ?? new JObject();
            _metadata = metadataJson;
        }

        /// <summary>
        /// Gets the version of the collection.
        /// </summary>
        public long Version => !TwinData.TryGetValue(VersionName, out JToken versionToken)
            ? default
            : (long)versionToken;

        /// <summary>
        /// Gets the count of properties in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                int count = TwinData.Count;
                if (count > 0)
                {
                    // Metadata and Version should not count towards this value
                    if (TwinData.TryGetValue(MetadataName, out _))
                    {
                        count--;
                    }

                    if (TwinData.TryGetValue(VersionName, out _))
                    {
                        count--;
                    }
                }

                return count;
            }
        }

        internal JObject TwinData { get; private set; }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Value for the given property name.</returns>
        /// <exception cref="InvalidOperationException">When the specified <paramref name="propertyName"/> does not exist in the collection.</exception>
        public dynamic this[string propertyName]
        {
            get
            {
                if (propertyName == MetadataName)
                {
                    return GetMetadata();
                }
                else if (propertyName == LastUpdatedName)
                {
                    return GetLastUpdatedOnUtc();
                }
                else if (propertyName == LastUpdatedVersionName)
                {
                    return GetLastUpdatedVersion();
                }
                else if (TryGetMemberInternal(propertyName, out dynamic value))
                {
                    return value;
                }

                throw new InvalidOperationException($"Unexpected property name '{propertyName}'.");
            }
            set => TrySetMemberInternal(propertyName, value);
        }

        /// <summary>
        /// Gets the metadata for this collection.
        /// </summary>
        public ClientTwinMetadata GetMetadata()
        {
            return new ClientTwinMetadata(GetLastUpdatedOnUtc(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Clears all metadata out of the twin collection as well as the base metadata object.
        /// </summary>
        /// <remarks>
        /// This will remove all metadata from the base metadata object as well as the metadata for the twin collection. 
        /// This will also clear the underlying metadata object which will affect methods such as
        /// <see cref="GetMetadata"/> and <see cref="GetLastUpdatedVersion"/>.
        /// </remarks>
        public void ClearMetadata()
        {
            TryClearMetadata(MetadataName);
            TryClearMetadata(LastUpdatedName);
            TryClearMetadata(LastUpdatedVersionName);
            TryClearMetadata(VersionName);

            // GitHub Issue: https://github.com/Azure/azure-iot-sdk-csharp/issues/1971
            // When we clear the metadata from the underlying collection we need to also clear
            // the _metadata object so the TryGetMemberInternal will return a JObject instead of a new TwinCollection
            _metadata.RemoveAll();
        }

        /// <summary>
        /// Gets the last time this property was updated in UTC.
        /// </summary>
        public DateTimeOffset GetLastUpdatedOnUtc()
        {
            if (_metadata != null
                && _metadata.TryGetValue(LastUpdatedName, out JToken lastUpdatedName)
                && (DateTimeOffset)lastUpdatedName is DateTimeOffset lastUpdatedOnUtc)
            {
                return lastUpdatedOnUtc;
            }

            return default;
        }

        /// <summary>
        /// Gets the last updated version for this property.
        /// </summary>
        /// <returns>Last updated version if present, null otherwise.</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata?[LastUpdatedVersionName];
        }

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns>true if the specified property is present; otherwise, false.</returns>
        public bool Contains(string propertyName)
        {
            return TwinData.ContainsKey(propertyName);
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

            if (!TwinData.TryGetValue(propertyName, out JToken jTokenValue))
            {
                return false;
            }

            // Try cast first
            try
            {
                propertyValue = jTokenValue.Value<T>();
                return true;
            }
            catch (InvalidCastException)
            { }

            // Otherwise, deserialize
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
        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, JToken> kvp in TwinData)
            {
                if (kvp.Key == MetadataName || kvp.Key == VersionName)
                {
                    continue;
                }

                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }

        /// <summary>
        /// The property payload as a JSON string.
        /// </summary>
        public override string ToString()
        {
            return TwinData.ToString();
        }

        /// <summary>
        /// Gets the specified property from the twin collection.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="result">The value to return from the property collection.</param>
        /// <returns>
        /// A <see cref="JToken"/> as an <see cref="object"/> if the metadata is not present; otherwise it will return a
        /// <see cref="ClientTwinProperties"/>, a <see cref="ClientTwinPropertyArray"/> or a <see cref="ClientTwinPropertyValue"/>.
        /// </returns>
        /// <remarks>
        /// If this method is used with a <see cref="ClientTwinProperties"/> returned from a <c>DeviceClient</c> it will always return a
        /// <see cref="JToken"/>. However, if you are using this method with a <see cref="ClientTwinProperties"/> returned from a
        /// <c>RegistryManager</c> client, it will return the corresponding type depending on what is stored in the properties collection.
        /// <para>
        /// For example a <see cref="List{T}"/> would return a <see cref="ClientTwinPropertyArray"/>, with the metadata intact, when used with
        /// a <see cref="ClientTwinProperties"/> returned from a <c>RegistryManager</c> client. If you need this method to always return a
        /// <see cref="JToken"/> please see the <see cref="ClearMetadata"/> method for more information.
        /// </para>
        /// </remarks>
        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            if (!TwinData.TryGetValue(propertyName, out JToken value))
            {
                result = null;
                return false;
            }

            if (_metadata?[propertyName] is JObject)
            {
                if (value is JValue jsonValue)
                {
                    result = new ClientTwinPropertyValue(jsonValue, (JObject)_metadata[propertyName]);
                }
                else if (value is JArray jsonArray)
                {
                    result = new ClientTwinPropertyArray(jsonArray, (JObject)_metadata[propertyName]);
                }
                else
                {
                    result = new ClientTwinProperties(value as JObject, (JObject)_metadata[propertyName]);
                }
            }
            else
            {
                // No metadata for this property, return as-is.
                // This is relevant for device client side operations.
                // The DeviceClient.GetTwinAsync() call returns only the desired and reported properties (with their corresponding version no.), without any metadata information.
                result = value;
            }

            return true;
        }

        private bool TrySetMemberInternal(string propertyName, object value)
        {
            JToken valueJToken = value == null
                ? null
                : JToken.FromObject(value);

            if (TwinData.TryGetValue(propertyName, out _))
            {
                TwinData[propertyName] = valueJToken;
            }
            else
            {
                TwinData.Add(propertyName, valueJToken);
            }

            return true;
        }

        private void TryClearMetadata(string propertyName)
        {
            if (Contains(propertyName))
            {
                TwinData.Remove(propertyName);
            }

            if (_metadata.ContainsKey(propertyName))
            {
                _metadata.Remove(propertyName);
            }
        }
    }
}
