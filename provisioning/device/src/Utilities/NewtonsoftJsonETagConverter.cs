// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// A <see cref="JsonConverter"/> implementation for <see cref="ETag"/>.
    /// </summary>
    internal sealed class NewtonsoftJsonETagConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType == typeof(ETag) || objectType == typeof(ETag?);

#nullable enable
        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (string?)reader.Value;
            if (value == null)
            {
                if (objectType == typeof(ETag?))
                {
                    return null;
                }

                return default(ETag);
            }
            return new ETag(value);
        }

#nullable disable

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var eTag = (ETag)value;
            if (eTag == default)
            {
                writer.WriteNull();
            }
            else
            {
                // Azure.Core.ETag expects the format "G" for serializing ETags that don't go into the header.
                // https://github.com/Azure/azure-sdk-for-net/blob/9c6238e0f0dd403d6583b56ec7902c77c64a2e37/sdk/core/Azure.Core/src/ETag.cs#L87-L114
                writer.WriteValue(eTag.ToString("G"));
            }
        }
    }
}
