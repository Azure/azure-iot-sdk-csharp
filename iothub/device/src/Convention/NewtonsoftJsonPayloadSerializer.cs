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

        /// <inheritdoc/>
        public override bool TryGetNestedJsonObjectValue<T>(object nestedObject, string propertyName, out T outValue)
        {
            outValue = default;
            if (nestedObject == null
                || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            try
            {
                // The supplied nested object is either a JObject or the string representation of a JObject.
                JObject nestedObjectAsJObject = nestedObject.GetType() == typeof(string)
                    ? DeserializeToType<JObject>((string)nestedObject)
                    : nestedObject as JObject;

                if (nestedObjectAsJObject != null
                    && nestedObjectAsJObject.TryGetValue(propertyName, out JToken element))
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

        /// <inheritdoc/>
        public override IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null)
        {
            return new NewtonsoftJsonWritablePropertyResponse(value, statusCode, version, description);
        }
    }
}
