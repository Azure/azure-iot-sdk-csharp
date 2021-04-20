// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The base class for all payloads that accept a <see cref="IPayloadConvention"/>
    /// </summary>
    public abstract class PayloadCollection : IEnumerable<object>
    {
        /// <summary>
        /// Get the value at the specified key
        /// </summary>
        /// <param name="key">Key of value</param>
        /// <remarks>
        /// This accessor is best used to access simple types. It is recommended to use <see cref="GetValue"/> to cast a complex type.
        /// </remarks>
        /// <returns>The specified property.</returns>
        public object this[string key]
        {
            get => Collection[key];
            set => Collection[key] = value;
        }

        /// <summary>
        /// The underlying collection for the payload.
        /// </summary>
        public IDictionary<string, object> Collection { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// The convention to use with this payload.
        /// </summary>
        public IPayloadConvention Convention { get; private set; }

        /// <summary>
        /// Default constructor for the class.
        /// </summary>
        /// <param name="payloadConvention">The convention used to serailize and encode this collection.</param>
        protected PayloadCollection(IPayloadConvention payloadConvention = default)
        {
            Convention = payloadConvention ?? DefaultPayloadConvention.Instance;
        }

        /// <summary>
        /// Gets the 
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Convention.PayloadSerializer.SerializeToString(Collection));
        }

        /// <summary>
        /// Gets the value of the object from the collection.
        /// </summary>
        /// <remarks>
        /// This class is used for both sending and receiving properties for the device and will have different behavior when casting. For example, if you're looking at the collection when used from <see cref="DeviceClient.GetPropertiesAsync(System.Threading.CancellationToken)"/> the stored objects will potetially be stored as a serialized object.
        /// </remarks>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="key">The key of the property to get.</param>
        /// <param name="useSerializer">Use the serializer to cast the object</param>
        /// <returns></returns>
        public virtual T GetValue<T>(string key, bool useSerializer = false)
        {
            // Use the serializer to attempt to cast the object correctly
            if (useSerializer)
            {
                return Convention.PayloadSerializer.CastFromObject<T>(Collection[key]);
            }
            return (T)Collection[key];
        }

        /// <summary>
        /// Returns a serailized string of this collection from the <see cref="ISerializer.SerializeToString(object)"/> method.
        /// </summary>
        /// <returns></returns>
        public virtual string GetSerailizedString()
        {
            return Convention.PayloadSerializer.SerializeToString(Collection);
        }

        ///  <inheritdoc />
        public IEnumerator<object> GetEnumerator()
        {
            foreach (object property in Collection)
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
