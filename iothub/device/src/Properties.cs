// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for properties.
    /// <remarks>
    /// The Properties class is not meant to be constructed by the customer code is meant to be returned from the <see cref="DeviceClient.GetPropertiesAsync(IPayloadConvention, System.Threading.CancellationToken)"/> method.
    /// </remarks>
    /// </summary>
    public class Properties : IEnumerable<object>
    {
        private const string VersionName = "$version";
        private readonly PropertyCollection _reportedPropertyCollection = new PropertyCollection();

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/>
        /// </summary>
        internal Properties()
        {
            Writable = new PropertyCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/> with the specified collections
        /// </summary>
        /// <param name="requestedPropertyCollection">A collection of writable properties returned from IoT Hub</param>
        /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
        internal Properties(PropertyCollection requestedPropertyCollection, PropertyCollection readOnlyPropertyCollection)
        {
            Writable = requestedPropertyCollection;
            _reportedPropertyCollection = readOnlyPropertyCollection;
        }

        /// <summary>
        /// The collection of writable properties
        /// </summary>
        /// <remarks>
        /// See the <see href="https://docs.microsoft.com/en-us/azure/iot-pnp/concepts-convention#writable-properties">Writable properties</see> documentation for more information.
        /// </remarks>
        public PropertyCollection Writable { get; private set; }

        /// <summary>
        /// Get the property from the propeties collection.
        /// </summary>
        /// <param name="key">The key of the property to get.</param>
        /// <remarks>
        /// This accessor is best used to access simple types. It is recommended to use <see cref="Get{T}(string)"/> to cast a complex type.
        /// </remarks>
        /// <returns>The specified property.</returns>
        public object this[string key]
        {
            get
            {
                return _reportedPropertyCollection[key];
            }
        }

        /// <summary>
        /// Determines whether the specified property is present.
        /// </summary>
        /// <param name="propertyName">The property to locate.</param>
        /// <returns>true if the specified property is present; otherwise, false</returns>
        public bool Contains(string propertyName)
        {
            return _reportedPropertyCollection.Collection.TryGetValue(propertyName, out _);
        }

        /// <summary>
        /// Gets the version of the properties.
        /// </summary>
        public long Version => _reportedPropertyCollection.Collection.TryGetValue(VersionName, out object version)
            ? (long)version
            : default;

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in _reportedPropertyCollection)
            {
                yield return property;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the property from the collection.
        /// </summary>
        /// <remarks>
        /// This calls <see cref="PayloadCollection.GetValue{T}(string)"/> and will use the serializer if needed. It is recommended to use this method over the <see cref="this[string]"/> accessor.
        /// </remarks>
        /// <typeparam name="T">The type to be returned.</typeparam>
        /// <param name="propertyKey">The key of the property to be returned.</param>
        /// <returns>A type of <typeparamref name="T"/> or <c>null</c></returns>
        public T Get<T>(string propertyKey)
        {
            return _reportedPropertyCollection.GetValue<T>(propertyKey);
        }

        /// <summary>
        /// Converts a <see cref="TwinProperties"/> collection to a properties collection.
        /// </summary>
        /// <param name="twinProperties">The TwinProperties object to convert.</param>
        /// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for the payload.</param>
        /// <returns>A new instance of of the class from an existing <see cref="TwinProperties"/> using an optional <see cref="IPayloadConvention"/>.</returns>
        internal static Properties FromTwinProperties(TwinProperties twinProperties, IPayloadConvention payloadConvention = default)
        {
            if (twinProperties == null)
            {
                throw new ArgumentNullException(nameof(twinProperties));
            }

            var writablePropertyCollection = PropertyCollection.FromTwinCollection(twinProperties.Desired, payloadConvention);
            foreach (KeyValuePair<string, object> property in twinProperties.Desired)
            {
                writablePropertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            writablePropertyCollection.Add(VersionName, twinProperties.Desired.Version);

            var propertyCollection = PropertyCollection.FromTwinCollection(twinProperties.Reported, payloadConvention);
            foreach (KeyValuePair<string, object> property in twinProperties.Reported)
            {
                propertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            propertyCollection.Add(VersionName, twinProperties.Reported.Version);

            return new Properties(writablePropertyCollection, propertyCollection);
        }
    }
}
