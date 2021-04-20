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
    /// This Properties class is not meant to be constructed by the customer code is meant to be returned from the <see cref="DeviceClient.GetPropertiesAsync(System.Threading.CancellationToken)"/> method.
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
        /// This accessor is best used to access simple types. It is recommended to use <see cref="Get{T}(string, bool)"/> to cast a complex type.
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
        /// We will use the <see cref="PayloadCollection.GetValue{T}(string, bool)"/> method to cast the object from a serailizer object by default. If you have constructed the class yourself and add items to the collections you will need to use <c>false</c> for the <paramref name="useSerializer"/> parameter.
        /// </remarks>
        /// <typeparam name="T">The type to be returned.</typeparam>
        /// <param name="propertyKey">The key of the property to be returned.</param>
        /// <param name="useSerializer">Use the serailizer to cast the property.</param>
        /// <returns>A type of <typeparamref name="T"/> or <c>null</c></returns>
        public T Get<T>(string propertyKey, bool useSerializer = true)
        {
            return _reportedPropertyCollection.GetValue<T>(propertyKey, useSerializer);
        }

        /// <summary>
        /// Converts a <see cref="TwinProperties"/> collection to a properties collection.
        /// </summary>
        /// <param name="twinProperties">The TwinProperties object to convert.</param>
        /// <returns></returns>
        internal static Properties FromTwinProperties(TwinProperties twinProperties)
        {
            if (twinProperties == null)
            {
                throw new ArgumentNullException(nameof(twinProperties));
            }

            var writablePropertyCollection = new PropertyCollection();
            foreach (KeyValuePair<string, object> property in twinProperties.Desired)
            {
                writablePropertyCollection.Add(property.Key, property.Value);
            }
            // The version information is not accessible via the enumerator, so assign it separately.
            writablePropertyCollection.Add(VersionName, twinProperties.Desired.Version);

            var propertyCollection = new PropertyCollection();
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
