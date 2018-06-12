// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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
        private const string MetadataName = "$metadata";
        private const string LastUpdatedName = "$lastUpdated";
        private const string LastUpdatedVersionName = "$lastUpdatedVersion";
        private const string VersionName = "$version";

        private JObject _jObject;
        private JObject _metadata;

        /// <summary>
        /// Creates instance of <see cref="TwinCollection"/>.
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
        /// Creates a <see cref="TwinCollection"/> using a JSON fragment as the body.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        internal TwinCollection(JObject twinJson)
        {
            _jObject = twinJson ?? new JObject();

            JToken metadataJToken;
            if (_jObject.TryGetValue(MetadataName, out metadataJToken))
            {
                _metadata = metadataJToken as JObject;
            }
        }

        /// <summary>
        /// Creates a <see cref="TwinCollection"/> using the given JSON fragments for the body and metadata.
        /// </summary>
        /// <param name="twinJson">JSON fragment containing the twin data.</param>
        /// <param name="metadataJson">JSON fragment containing the metadata.</param>
        public TwinCollection(JObject twinJson, JObject metadataJson)
        {
            _jObject = twinJson ?? new JObject();
            _metadata = metadataJson;
        }

        /// <summary>
        /// Gets the version of the <see cref="TwinCollection"/>
        /// </summary>
        public long Version
        {
            get
            {
                JToken versionToken;
                if (!_jObject.TryGetValue(VersionName, out versionToken))
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
                int count = _jObject.Count;
                if (count > 0)
                {
                    // Metadata and Version should not count towards this value
                    JToken ignored;
                    if (_jObject.TryGetValue(MetadataName, out ignored))
                    {
                        count--;
                    }

                    if (_jObject.TryGetValue(VersionName, out ignored))
                    {
                        count--;
                    }
                }

                return count;
            }
        }

        internal JObject JObject => _jObject;

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
                dynamic value;
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
                else if (TryGetMemberInternal(propertyName, out value))
                {
                    return value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(propertyName));
                }
            }
            set { TrySetMemberInternal(propertyName, value); }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _jObject.ToString();
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
            return (long?)_metadata[LastUpdatedVersionName];
        }

        /// <summary>
        /// Gets the TwinProperties as a JSON string
        /// </summary>
        /// <param name="formatting">Optional. Formatting for the output JSON string.</param>
        /// <returns>JSON string</returns>
        public string ToJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(_jObject, formatting);
        }

        /// <summary>
        /// Determines whether the specified property is present
        /// </summary>
        /// <param name="propertyName">The property to locate</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            JToken ignored;
            return _jObject.TryGetValue(propertyName, out ignored);
        }

        /// <inheritdoc />
        public IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, JToken> kvp in _jObject)
            {
                if (kvp.Key == MetadataName || kvp.Key == VersionName)
                {
                    continue;
                }

                yield return new KeyValuePair<string, dynamic>(kvp.Key, this[kvp.Key]);
            }
        }

        private bool TryGetMemberInternal(string propertyName, out object result)
        {
            JToken value;
            if (!_jObject.TryGetValue(propertyName, out value))
            { 
                result = null;
                return false;
            }
            
            if (_metadata?[propertyName] is JObject)
            {
                if (value is JValue)
                {
                    result = new TwinCollectionValue((JValue)value, (JObject)_metadata[propertyName]);
                }
                else
                {
                    result = new TwinCollection(value as JObject, (JObject)_metadata[propertyName]);
                }
            }
            else
            {
                // No metadata for this property, return as-is.
                result = value;
            }

            return true;
        }

        private bool TrySetMemberInternal(string propertyName, object value)
        {
            JToken valueJToken =  value == null ? null : JToken.FromObject(value);
            JToken ignored;
            if (_jObject.TryGetValue(propertyName, out ignored))
            {
                _jObject[propertyName] = valueJToken;
            }
            else
            {
                _jObject.Add(propertyName, valueJToken);
            }

            return true;
        }

        private void TryClearMetadata(string propertyName)
        {
            JToken ignored;
            if (_jObject.TryGetValue(propertyName, out ignored))
            {
                _jObject.Remove(propertyName);
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

    }
}
