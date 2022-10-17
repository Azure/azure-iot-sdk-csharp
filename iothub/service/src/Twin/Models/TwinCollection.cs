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
    /// Represents a collection of properties for the twin.
    /// </summary>
    [JsonConverter(typeof(TwinCollectionJsonConverter))]
    public class TwinCollection : IEnumerable
    {
        internal const string MetadataName = "$metadata";
        internal const string LastUpdatedName = "$lastUpdated";
        internal const string LastUpdatedVersionName = "$lastUpdatedVersion";
        internal const string VersionName = "$version";

        private readonly JObject _metadata;

        /// <summary>
        /// Creates an instance of this class.
        /// Shouldn't use this constructor since metadata object is null and calling GetLastUpdated can result in NullReferenceException.
        /// </summary>
        public TwinCollection()
            : this((JObject)null)
        {
        }

        /// <summary>
        /// Creates an instance of this class using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        public TwinCollection(string twinJson)
            : this(JObject.Parse(twinJson))
        {
        }

        /// <summary>
        /// Creates an instance of this class using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public TwinCollection(string twinJson, string metadataJson)
            : this(JObject.Parse(twinJson), JObject.Parse(metadataJson))
        {
        }

        /// <summary>
        /// Creates an instance of this class using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        internal TwinCollection(JObject twinJson)
        {
            JObject = twinJson ?? new JObject();

            if (JObject.TryGetValue(MetadataName, out JToken metadataJToken))
            {
                _metadata = metadataJToken as JObject;
            }
        }

        /// <summary>
        /// Creates an instance of this class using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public TwinCollection(JObject twinJson, JObject metadataJson)
        {
            JObject = twinJson ?? new JObject();
            _metadata = metadataJson;
        }

        /// <summary>
        /// Gets the version of the collection.
        /// </summary>
        public long Version => !JObject.TryGetValue(VersionName, out JToken versionToken)
            ? default
            : (long)versionToken;

        /// <summary>
        /// Gets the count of properties in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                int count = JObject.Count;
                if (count > 0)
                {
                    // Metadata and Version should not count towards this value
                    if (JObject.TryGetValue(MetadataName, out _))
                    {
                        count--;
                    }

                    if (JObject.TryGetValue(VersionName, out _))
                    {
                        count--;
                    }
                }

                return count;
            }
        }

        internal JObject JObject { get; private set; }

        /// <summary>
        /// Property indexer.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Value for the given property name.</returns>
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
                    return GetLastUpdatedOn();
                }
                else if (propertyName == LastUpdatedVersionName)
                {
                    return GetLastUpdatedVersion();
                }
                else if (TryGetMemberInternal(propertyName, out dynamic value))
                {
                    return value;
                }

                throw new ArgumentException($"Unexpected property name '{propertyName}'.", nameof(propertyName));
            }
            set => TrySetMemberInternal(propertyName, value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return JObject.ToString();
        }

        /// <summary>
        /// Gets the metadata for this property.
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property.</returns>
        public TwinMetadata GetMetadata()
        {
            return new TwinMetadata(GetLastUpdatedOn(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the last updated time for this property.
        /// </summary>
        /// <returns>Date-time instance representing the last updated time for this property.</returns>
        /// <exception cref="NullReferenceException">Thrown when the metadata object is null.
        /// An example would be when the this class is created with the default constructor.</exception>
        public DateTimeOffset GetLastUpdatedOn()
        {
            return (DateTime)_metadata[LastUpdatedName];
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
        /// Gets the properties as a JSON string.
        /// </summary>
        /// <param name="formatting">Optional. Formatting for the output JSON string.</param>
        /// <returns>JSON string.</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(JObject, formatting);
        }

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns>true if the specified property is present; otherwise, false.</returns>
        public bool Contains(string propertyName)
        {
            return JObject.TryGetValue(propertyName, out _);
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, JToken> kvp in JObject)
            {
                if (kvp.Key == MetadataName || kvp.Key == VersionName)
                {
                    continue;
                }

                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }

        /// <summary>
        /// Gets the specified property from the twin collection.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="result">The value to return from the property collection.</param>
        /// <returns>
        /// A <see cref="JToken"/> as an <see cref="object"/> if the metadata is not present; otherwise it will return a
        /// <see cref="TwinCollection"/>, a <see cref="TwinCollectionArray"/> or a <see cref="TwinCollectionValue"/>.
        /// </returns>
        /// <remarks>
        /// If this method is used with a <see cref="TwinCollection"/> returned from a <c>DeviceClient</c> it will always return a
        /// <see cref="JToken"/>. However, if you are using this method with a <see cref="TwinCollection"/> returned from a
        /// <c>RegistryManager</c> client, it will return the corresponding type depending on what is stored in the properties collection.
        ///
        /// For example a <see cref="List{T}"/> would return a <see cref="TwinCollectionArray"/>, with the metadata intact, when used with
        /// a <see cref="TwinCollection"/> returned from a <c>RegistryManager</c> client. If you need this method to always return a
        /// <see cref="JToken"/> please see the <see cref="ClearAllMetadata"/> method for more information.
        /// </remarks>
        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            if (!JObject.TryGetValue(propertyName, out JToken value))
            {
                result = null;
                return false;
            }

            if (_metadata?[propertyName] is JObject)
            {
                if (value is JValue jsonValue)
                {
                    result = new TwinCollectionValue(jsonValue, (JObject)_metadata[propertyName]);
                }
                else if (value is JArray jsonArray)
                {
                    result = new TwinCollectionArray(jsonArray, (JObject)_metadata[propertyName]);
                }
                else
                {
                    result = new TwinCollection(value as JObject, (JObject)_metadata[propertyName]);
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
            JToken valueJToken = value == null ? null : JToken.FromObject(value);
            if (JObject.TryGetValue(propertyName, out _))
            {
                JObject[propertyName] = valueJToken;
            }
            else
            {
                JObject.Add(propertyName, valueJToken);
            }

            return true;
        }

        private void TryClearMetadata(string propertyName)
        {
            if (JObject.TryGetValue(propertyName, out _))
            {
                JObject.Remove(propertyName);
            }
        }

        /// <summary>
        /// Clears metadata out of the twin collection.
        /// </summary>
        /// <remarks>
        /// This will only clear the metadata from the twin collection but will not change the base metadata object. This allows you to still use
        /// methods such as <see cref="GetMetadata"/>. If you need to remove all metadata, please use <see cref="ClearAllMetadata"/>.
        /// </remarks>
        public void ClearMetadata()
        {
            TryClearMetadata(MetadataName);
            TryClearMetadata(LastUpdatedName);
            TryClearMetadata(LastUpdatedVersionName);
            TryClearMetadata(VersionName);
        }

        /// <summary>
        /// Clears all metadata out of the twin collection as well as the base metadata object.
        /// </summary>
        /// <remarks>
        /// This will remove all metadata from the base metadata object as well as the metadata for the twin collection. The difference from the
        /// <see cref="ClearMetadata"/> method is this will also clear the underlying metadata object which will affect methods such as
        /// <see cref="GetMetadata"/> and <see cref="GetLastUpdatedVersion"/>.
        /// This method would be useful if you are performing any operations that require <see cref="TryGetMemberInternal(string, out object)"/>
        /// to return a <see cref="JToken"/> regardless of the client you are using.
        /// </remarks>
        public void ClearAllMetadata()
        {
            ClearMetadata();
            // GitHub Issue: https://github.com/Azure/azure-iot-sdk-csharp/issues/1971
            // When we clear the metadata from the underlying collection we need to also clear
            // the _metadata object so the TryGetMemberInternal will return a JObject instead of a new TwinCollection
            _metadata.RemoveAll();
        }
    }
}
