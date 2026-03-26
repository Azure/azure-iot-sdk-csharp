// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of twin properties.
    /// </summary>
    public class PropertyCollection : JsonDictionary
    {
        private const string VersionName = "$version";

        /// <summary>
        /// 
        /// </summary>
        public PropertyCollection() : this(new(), false)
        { 
        
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        /// <param name="properties">The properies that will be set to this collection. These can be either client reported properties or
        /// property update requests received from service.</param>
        /// <param name="responseFromService">A flag that indicates if this property collection was received from service or if it was user-constructed.</param>
        public PropertyCollection(Dictionary<string, object> properties, bool responseFromService = false) : base(properties)
        {
            {
                // A property collection reecived from service, i.e. a response of GetTwinAsync() call or a desired property update
                // notification will have the version field populated. A user constructed property collection is not expected
                // to have the version field populated.
                if (responseFromService)
                {
                    // The version information should not be a part of the enumerable ProperyCollection, but rather should be
                    // accessible through its dedicated accessor.
                    bool versionPresent = base.TryGetAndDeserializeValue(VersionName, out long? version);

                    Version = versionPresent
                        ? version.Value
                        : throw new IotHubClientException("Properties document either missing version number or not formatted as expected. Contact service with logs.");
                }
            }
        }

        /// <summary>
        /// The version of the client twin properties.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the client twin properties.</value>
        public long Version { get; private protected set; }

        /// <summary>
        /// The client twin properties, as a serialized string.
        /// </summary>
        public string GetSerializedString()
        {
            return JsonSerializer.Serialize(Properties, JsonSerializerSettings.Options);
        }

        /// <inheritdoc/>
        public new IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var key in Properties.Keys)
            {
                if (!key.Equals(VersionName, System.StringComparison.Ordinal))
                { 
                    yield return new KeyValuePair<string, object>(key, FromJsonElement(Properties[key]));
                }
            }
        }
    }
}
