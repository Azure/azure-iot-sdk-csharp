// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A <see cref="JsonConvert"/> PayloadSerializer implementation.
    /// </summary>
    public class NewtonsoftJsonPayloadSerializer : PayloadSerializer
    {
        /// <summary>
        /// The content type string.
        /// </summary>
        internal const string ApplicationJson = "application/json";

        private NewtonsoftJsonPayloadSerializer()
        {

        }

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        public static NewtonsoftJsonPayloadSerializer Instance { get; } = new NewtonsoftJsonPayloadSerializer();

        /// <inheritdoc/>
        public override string ContentType => ApplicationJson;

        /// <inheritdoc/>
        public override string SerializeToString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        /// <inheritdoc/>
        public override T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonConvert.DeserializeObject<T>(stringToDeserialize);
        }

        /// <inheritdoc/>
        public override T ConvertFromJsonObject<T>(object objectToConvert)
        {
            var token = JToken.FromObject(objectToConvert);

            return objectToConvert == null
                ? default
                : token.ToObject<T>();
        }

        /// <summary>
        /// Gets a nested property from the serialized JSON data.
        /// </summary>
        /// <remarks>
        /// This class is usedto attempt to get a property of the underlying JSON object.
        /// </remarks>
        /// <typeparam name="T">The type to convert the retrieved property to.</typeparam>
        /// <param name="nestedJsonObject">The object that might contain the nested property.
        /// This needs to be in the json object equivalent format as required by the serializer or the string representation of it.</param>
        /// <param name="propertyName">The name of the property to be retrieved.</param>
        /// <param name="outValue">The retrieved value.</param>
        /// <returns>True if the nested object contains an element with the specified key, otherwise false.</returns>
        internal static bool TryGetNestedJsonObjectValue<T>(JObject nestedJsonObject, string propertyName, out T outValue)
        {
            outValue = default;
            if (nestedJsonObject == null
                || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                if (nestedJsonObject != null
                    && nestedJsonObject.TryGetValue(propertyName, out JToken element))
                {
                    outValue = element.ToObject<T>();
                    return true;
                }
            }
            catch
            {
                // Catch and ignore any exceptions caught
            }

            return false;
        }
    }
}
