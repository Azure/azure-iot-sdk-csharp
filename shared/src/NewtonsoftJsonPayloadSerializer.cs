// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A <see cref="JsonConvert"/> <see cref="PayloadSerializer"/> implementation.
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
        public static readonly NewtonsoftJsonPayloadSerializer Instance = new NewtonsoftJsonPayloadSerializer();

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
        public override T ConvertFromObject<T>(object objectToConvert)
        {
            if (objectToConvert == null)
            {
                return default;
            }
            return ((JToken)objectToConvert).ToObject<T>();
        }

        /// <inheritdoc/>
        public override bool TryGetNestedObjectValue<T>(object nestedObject, string propertyName, out T outValue)
        {
            outValue = default;
            if (nestedObject == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }
            if (((JObject)nestedObject).TryGetValue(propertyName, out JToken element))
            {
                outValue = element.ToObject<T>();
                return true;
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
