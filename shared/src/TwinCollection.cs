﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Represents a collection of properties for <see cref="Twin"/>
    /// </summary>
    [SuppressMessage(
        "Microsoft.Design",
        "CA1010:CollectionsShouldImplementGenericInterface",
        Justification = "Public API: this was not designed to be a generic collection.")]
    [JsonConverter(typeof(TwinCollectionJsonConverter))]
    public class TwinCollection : IEnumerable
    {
        internal const string MetadataName = "$metadata";
        internal const string LastUpdatedName = "$lastUpdated";
        internal const string LastUpdatedVersionName = "$lastUpdatedVersion";
        internal const string VersionName = "$version";
        private readonly JObject _metadata;

        /// <summary>
        /// Creates instance of <see cref="TwinCollection"/>.
        /// Shouldn't use this constructor since _metadata is null and calling GetLastUpdated can result in NullReferenceException
        /// </summary>
        public TwinCollection()
            : this((JObject)null)
        {
        }

        /// <summary>
        /// Creates a <see cref="TwinCollection"/> using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        public TwinCollection(string twinJson)
            : this(JObject.Parse(twinJson))
        {
        }

        /// <summary>
        /// Creates a <see cref="TwinCollection"/> using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public TwinCollection(string twinJson, string metadataJson)
            : this(JObject.Parse(twinJson), JObject.Parse(metadataJson))
        {
        }

        /// <summary>
        /// Creates a <see cref="TwinCollection"/> using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public TwinCollection(JObject twinJson, JObject metadataJson)
        {
            JObject = twinJson ?? new JObject();
            _metadata = metadataJson;
        }

        /// <summary>
        /// Creates a <see cref="TwinCollection"/> using a JSON fragment as the body.
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
        /// Gets the version of the <see cref="TwinCollection"/>
        /// </summary>
        public long Version
        {
            get
            {
                if (!JObject.TryGetValue(VersionName, out JToken versionToken))
                {
                    return default(long);
                }

                return (long)versionToken;
            }
        }

        /// <summary>
        /// Gets the count of properties in the Collection
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

        /// <summary>
        /// Property Indexer
        /// </summary>
        /// <param name="propertyName">Name of the property to get</param>
        /// <returns>Value for the given property name</returns>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations",
            Justification = "AppCompat. Changing the exception to ArgumentException might break existing applications.")]
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
                    return GetLastUpdated();
                }
                else if (propertyName == LastUpdatedVersionName)
                {
                    return GetLastUpdatedVersion();
                }
                else if (TryGetMemberInternal(propertyName, out dynamic value))
                {
                    return value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(propertyName));
                }
            }
            set => TrySetMemberInternal(propertyName, value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return JObject.ToString();
        }

        /// <summary>
        /// Gets the Metadata for this property
        /// </summary>
        /// <returns>Metadata instance representing the metadata for this property</returns>
        public Metadata GetMetadata()
        {
            return new Metadata(GetLastUpdated(), GetLastUpdatedVersion());
        }

        /// <summary>
        /// Gets the LastUpdated time for this property
        /// </summary>
        /// <returns>DateTime instance representing the LastUpdated time for this property</returns>
        /// <exception cref="System.NullReferenceException">Thrown when the TwinCollection metadata is null.
        /// An example would be when the TwinCollection class is created with the default constructor</exception>
        public DateTime GetLastUpdated()
        {
            return (DateTime)_metadata[LastUpdatedName];
        }

        /// <summary>
        /// Gets the LastUpdatedVersion for this property
        /// </summary>
        /// <returns>LastUpdatdVersion if present, null otherwise</returns>
        public long? GetLastUpdatedVersion()
        {
            return (long?)_metadata?[LastUpdatedVersionName];
        }

        /// <summary>
        /// Gets the TwinProperties as a JSON string
        /// </summary>
        /// <param name="formatting">Optional. Formatting for the output JSON string.</param>
        /// <returns>JSON string</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(JObject, formatting);
        }

        /// <summary>
        /// Determines whether the specified property is present
        /// </summary>
        /// <param name="propertyName">The property to locate</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            return JObject.TryGetValue(propertyName, out JToken ignored);
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
        /// Clear metadata out of the collection
        /// </summary>
        public void ClearMetadata()
        {
            TryClearMetadata(MetadataName);
            TryClearMetadata(LastUpdatedName);
            TryClearMetadata(LastUpdatedVersionName);
            TryClearMetadata(VersionName);
        }

        internal JObject JObject { get; private set; }

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
    }
}
