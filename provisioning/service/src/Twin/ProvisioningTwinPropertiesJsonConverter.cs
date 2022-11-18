// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ProvisioningTwinPropertiesJsonConverter : JsonConverter<ProvisioningTwinProperties>
    {
        public override bool CanConvert(Type objectType) => typeof(ProvisioningTwinProperties).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        public override ProvisioningTwinProperties? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new ProvisioningTwinProperties(JToken.ReadFrom(reader) as JObject);
        }

        public override void Write(Utf8JsonWriter writer, ProvisioningTwinProperties value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var properties = value;
            if (properties == null)
            {
                throw new InvalidOperationException("Object passed is not of type TwinCollection.");
            }

            serializer.Serialize(writer, properties.JObject);
        }
    }
}

