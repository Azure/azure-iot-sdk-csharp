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
    /// The Properties class is not meant to be constructed by customer code. It is intended to be returned fully popualated from the <see cref="DeviceClient.GetPropertiesAsync(PayloadConvention, System.Threading.CancellationToken)"/> method.
    /// </remarks>
    /// </summary>
    public class ClientProperties : IEnumerable<object>
    {
        private readonly ClientPropertyCollection _reportedPropertyCollection = new ClientPropertyCollection();

        /// <summary>
        /// Initializes a new instance of <see cref="ClientProperties"/>
        /// </summary>
        internal ClientProperties()
        {
            Writable = new ClientPropertyCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ClientProperties"/> with the specified collections
        /// </summary>
        /// <param name="requestedPropertyCollection">A collection of writable properties returned from IoT Hub</param>
        /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
        internal ClientProperties(ClientPropertyCollection requestedPropertyCollection, ClientPropertyCollection readOnlyPropertyCollection)
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
        public ClientPropertyCollection Writable { get; private set; }

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
        public long Version => _reportedPropertyCollection.Version;

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

        internal static ClientProperties FromClientTwinProperties(ClientTwinProperties clientTwinProperties, PayloadConvention payloadConvention)
        {
            if (clientTwinProperties == null)
            {
                throw new ArgumentNullException(nameof(clientTwinProperties));
            }

            payloadConvention ??= DefaultPayloadConvention.Instance;

            var writablePropertyCollection = ClientPropertyCollection.FromClientTwinDictionary(clientTwinProperties.Desired, payloadConvention);
            var propertyCollection = ClientPropertyCollection.FromClientTwinDictionary(clientTwinProperties.Reported, payloadConvention);

            return new ClientProperties(writablePropertyCollection, propertyCollection);
        }
    }
}
