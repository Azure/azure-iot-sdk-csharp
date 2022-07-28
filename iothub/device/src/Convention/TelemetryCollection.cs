// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The telemetry collection used to populate a <see cref="TelemetryMessage"/>.
    /// </summary>
    public class TelemetryCollection : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The underlying collection for the payload.
        /// </summary>
        internal IDictionary<string, object> Collection { get; } = new Dictionary<string, object>();

        /// <summary>
        /// The convention to use with this telemetry collection.
        /// </summary>
        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <remarks>
        /// This accessor is best used to access and cast to simple types.
        /// It is recommended to use <see cref="TryGetValue"/> to deserialize to a complex type.
        /// </remarks>
        /// <param name="telemetryKey">The name of the telemetry.</param>
        /// <returns>The value of the telemetry.</returns>
        public object this[string telemetryKey]
        {
            get => Collection[telemetryKey];
            set => Add(telemetryKey, value);
        }

        /// <summary>
        /// Adds or updates an entry in the telemetry collection.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry.</param>
        /// <param name="telemetryValue">The value of the telemetry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryName"/> is <c>null</c> </exception>
        public void Add(string telemetryName, object telemetryValue)
        {
            Collection[telemetryName] = telemetryValue;
        }

        /// <summary>
        /// Adds or updates the telemetry collection.
        /// </summary>
        /// <inheritdoc cref="Add(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="Add(string, object)" path="/param['telemetryValue']"/>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryValues"/> is <c>null</c>.</exception>
        public void Add(IDictionary<string, object> telemetryValues)
        {
            if (telemetryValues == null)
            {
                throw new ArgumentNullException(nameof(telemetryValues));
            }

            telemetryValues
                .ToList()
                .ForEach(entry => Collection[entry.Key] = entry.Value);
        }

        /// <summary>
        /// Determines whether the specified telemetry key is present.
        /// </summary>
        /// <param name="key">The telemetry key in the collection to locate.</param>
        /// <returns><c>true</c> if the specified property is present, otherwise <c>false</c>.</returns>
        public bool Contains(string key)
        {
            return Collection.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value of telemetry corresponding to the specified key from the collection.
        /// </summary>
        /// <typeparam name="T">The type to cast the object to.</typeparam>
        /// <param name="telemetryKey">The key of the telemetry to get.</param>
        /// <param name="telemetryValue">When this method returns true, this contains the value of the object from the collection.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a value of type <c>T</c> with the specified key was found; otherwise <c>false</c>.</returns>
        public bool TryGetValue<T>(string telemetryKey, out T telemetryValue)
        {
            telemetryValue = default;

            // If the key is null, empty or whitespace, then return false with the default value of the type <T> passed in.
            if (string.IsNullOrWhiteSpace(telemetryKey))
            {
                return false;
            }

            if (Collection.ContainsKey(telemetryKey))
            {
                object retrievedValue = Collection[telemetryKey];

                // If the object is of type T or can be cast to type T, go ahead and return it.
                if (ObjectConversionHelpers.TryCast(retrievedValue, out telemetryValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove all items from the collection.
        /// </summary>
        public void ClearCollection()
        {
            Collection.Clear();
        }

        /// <summary>
        /// The telemetry collection, as a serialized string.
        /// </summary>
        public string GetSerializedString()
        {
            return Convention.PayloadSerializer.SerializeToString(Collection);
        }

        ///  <inheritdoc />
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (KeyValuePair<string, object> telemetry in Collection)
            {
                yield return telemetry;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the collection as a byte array.
        /// </summary>
        /// <remarks>
        /// This will get the fully encoded serialized string using both <see cref="PayloadSerializer.SerializeToString(object)"/>.
        /// and <see cref="PayloadEncoder.EncodeStringToByteArray(string)"/> methods implemented in the <see cref="PayloadConvention"/>.
        /// </remarks>
        /// <returns>A fully encoded serialized string.</returns>
        internal virtual byte[] GetPayloadObjectBytes()
        {
            return Convention.GetObjectBytes(Collection);
        }
    }
}