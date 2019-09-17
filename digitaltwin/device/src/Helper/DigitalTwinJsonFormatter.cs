// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Iot.DigitalTwin.Device.Helper
{
    /// <summary>
    /// The Json Serializer.
    /// </summary>
    internal class DigitalTwinJsonFormatter : IDigitalTwinFormatter
    {
        /// <summary>
        /// Serialize to string.
        /// </summary>
        /// <typeparam name="T">Any class or struct.</typeparam>
        /// <param name="obj">The object needs to be serialized.</param>
        /// <returns>The serialized string.</returns>
        public string FromObject<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// Serialize to string.
        /// </summary>
        /// <typeparam name="T">Any class or struct.</typeparam>
        /// <param name="value">The string.</param>
        /// <returns>The instance needs to be de-serialized.</returns>
        public T ToObject<T>(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default(T);
            }

            T result = JsonConvert.DeserializeObject<T>(value);

            // If consumer pass Object or dynamic as T and result is a complex json object,
            // in order to represent as JObject, we represent as DataCollection.
            if (typeof(T) == typeof(object) && result != null && result.GetType() == typeof(JObject))
            {
                result = (T)(new DataCollection(value) as object);
            }

            return result;
        }
    }
}
