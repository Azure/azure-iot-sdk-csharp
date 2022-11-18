// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    internal class ClientTwinPropertiesJsonConverter : JsonConverter<ClientTwinProperties>
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => typeof(ClientTwinProperties).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        /// <inheritdoc/>
        public override ClientTwinProperties? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new ClientTwinProperties(JToken.ReadFrom(reader) as JObject);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ClientTwinProperties value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is not ClientTwinProperties properties)
            {
                throw new InvalidOperationException("Object passed is not of type TwinCollection.");
            }

            serializer.Serialize(writer, properties.JObject);
        }
    }
}
