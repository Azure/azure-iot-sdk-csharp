// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET451

using System.Text.Json;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A <see cref="System.Text.Json"/> <see cref="PayloadSerializer"/> implementation.
    /// </summary>
    public class SystemTextJsonPayloadSerializer : PayloadSerializer
    {
        /// <summary>
        /// The Content Type string.
        /// </summary>
        internal const string ApplicationJson = "application/json";

        /// <summary>
        /// The default instance of this class.
        /// </summary>
        public static readonly SystemTextJsonPayloadSerializer Instance = new SystemTextJsonPayloadSerializer();

        /// <inheritdoc/>
        public override string ContentType => ApplicationJson;

        /// <inheritdoc/>
        public override string SerializeToString(object objectToSerialize)
        {
            return JsonSerializer.Serialize(objectToSerialize);
        }

        /// <inheritdoc/>
        public override T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonSerializer.Deserialize<T>(stringToDeserialize);
        }

        /// <inheritdoc/>
        public override T ConvertFromObject<T>(object objectToConvert)
        {
            return DeserializeToType<T>(SerializeToString(objectToConvert));
        }

        /// <inheritdoc/>
        public override bool TryGetNestedObjectValue<T>(object nestedObject, string propertyName, out T outValue)
        {
            outValue = default;
            if (nestedObject == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }
            if (((JsonElement)nestedObject).TryGetProperty(propertyName, out JsonElement element))
            {
                outValue = DeserializeToType<T>(element.GetRawText());
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null)
        {
            return new SystemTextJsonWritablePropertyResponse(value, statusCode, version, description);
        }
    }
}

#endif
