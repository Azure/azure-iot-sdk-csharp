// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
#if NETSTANDARD1_3
    using System.Reflection;
#endif

    using Microsoft.Azure.Devices.Common;
    using Microsoft.Azure.Devices.Shared;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class TwinContentJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            TwinContent twinContent = value as TwinContent;

            if (twinContent == null)
            {
                throw new InvalidOperationException("Invalid TwinContent object");
            }

            writer.WriteStartObject();
            writer.WritePropertyName(twinContent.TargetPropertyPath);
            writer.WriteRawValue(JsonConvert.SerializeObject(twinContent.TargetContent));
            writer.WriteEndObject();
        }

        public override bool CanConvert(Type objectType) =>
#if NETSTANDARD1_3
            typeof(TwinContent).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
#else
            typeof(TwinContent).IsAssignableFrom(objectType);
#endif

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.None)
            {
                reader.Read();
            }

            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidOperationException("Invalid TwinContent JSON.");
            }

            TwinContent twinContent = new TwinContent();
            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        throw new InvalidOperationException();
                    }

                    string propertyName = reader.Value as string;
                    twinContent.TargetPropertyPath = propertyName;

                    reader.Read();
                    JObject obj = JObject.Load(reader);
                    twinContent.TargetContent = obj.ToObject<TwinCollection>();
                }
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidOperationException("Error parsing TwinContent JSON: {0}".FormatInvariant(ex.Message));
            }

            return twinContent;
        }
    }
}