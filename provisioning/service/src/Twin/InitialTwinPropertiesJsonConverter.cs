﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class InitialTwinPropertiesJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is not InitialTwinPropertyCollection properties)
            {
                throw new InvalidOperationException("Object passed is not of type TwinCollection.");
            }

            serializer.Serialize(writer, properties.JObject);
        }

        public override bool CanConvert(Type objectType) => typeof(InitialTwinPropertyCollection).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new InitialTwinPropertyCollection(JToken.ReadFrom(reader) as JObject);
        }
    }
}

