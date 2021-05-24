// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET451

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A <see cref="System.Text.Json"/> <see cref="PayloadSerializer"/> implementation.
    /// </summary>
    public class SystemTextJsonPayloadSerializer : PayloadSerializer
    {
        /// <summary>
        /// Class taken from https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#deserialize-inferred-types-to-object-properties
        /// </summary>
        internal class ObjectToInferredTypesConverter : JsonConverter<object>
        {
            public override object Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options) => reader.TokenType switch
                {
                    JsonTokenType.True => true,
                    JsonTokenType.False => false,
                    JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
                    JsonTokenType.Number => reader.GetDouble(),
                    JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
                    JsonTokenType.String => reader.GetString(),
                    _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
                };

            public override void Write(
                Utf8JsonWriter writer,
                object objectToWrite,
                JsonSerializerOptions options) =>
                JsonSerializer.Serialize(writer, objectToWrite, objectToWrite.GetType(), options);
        }
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
            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new ObjectToInferredTypesConverter());
            return JsonSerializer.Deserialize<T>(stringToDeserialize, jsonOptions);
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
