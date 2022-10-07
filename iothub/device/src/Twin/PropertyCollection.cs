// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of twin properties.
    /// </summary>
    /// <remarks>
    /// This abstract class is inherited by <see cref="ReportedPropertyCollection"/> to represent the collection of twin properties reported by the client.
    /// This abstract class is inherited by <see cref="DesiredPropertyCollection"/> to represent the collection of desired property update requests received from service.
    /// </remarks>
    public abstract class PropertyCollection : IEnumerable<KeyValuePair<string, object>>
    {
        private const string VersionName = "$version";

        // Visibility of this type is restricted to this class or types derived from this class within our assembly.
        private protected readonly Dictionary<string, object> _properties = new();

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal PropertyCollection(Dictionary<string, object> properties)
        {
            Argument.AssertNotNull(properties, nameof(properties));

            // The version information should not be a part of the enumerable ProperyCollection, but rather should be
            // accessible through its dedicated accessor.
            bool versionPresent = properties.TryGetValue(VersionName, out object version);

            Version = versionPresent && ObjectConversionHelper.TryCastNumericTo(version, out long longVersion)
                ? longVersion
                : throw new IotHubClientException("Properties document either missing version number or not formatted as expected. Contact service with logs.", false);

            foreach (KeyValuePair<string, object> property in properties)
            {
                // Ignore the version entry since we've already saved it off.
                if (property.Key == VersionName)
                {
                    // no-op
                }
                else
                {
                    _properties.Add(property.Key, property.Value);
                }
            }
        }

        /// <summary>
        /// The version of the client twin properties.
        /// </summary>
        /// <value>A <see cref="long"/> that is used to identify the version of the client twin properties.</value>
        public long Version { get; private protected set; }

        /// <summary>
        /// The payload convention that defines a specific serializer as well as a specific content encoding for the payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; }

        /// <summary>
        /// Gets the value associated with the <paramref name="propertyKey"/> in the reported property collection.
        /// </summary>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="propertyKey">The key of the property to get.</param>
        /// <param name="propertyValue">When this method returns true, this contains the value of the object from the collection.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns>True if a value of type <c>T</c> with the specified key was found; otherwise, it returns false.</returns>
        public bool TryGetValue<T>(string propertyKey, out T propertyValue)
        {
            propertyValue = default;

            if (_properties.ContainsKey(propertyKey))
            {
                if (ObjectConversionHelper.TryCastOrConvert<T>(_properties[propertyKey], PayloadConvention, out propertyValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The client twin properties, as a serialized string.
        /// </summary>
        public string GetSerializedString()
        {
            return PayloadConvention.PayloadSerializer.SerializeToString(_properties);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> property in _properties)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
