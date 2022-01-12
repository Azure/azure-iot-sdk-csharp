// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "CodeAnalysis limitation: TwinCollectionJsonConverter is actually used by TwinCollection")]
    internal class TwinCollectionJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var properties = value as TwinCollection;
            if (properties == null)
            {
                throw new InvalidOperationException("Object passed is not of type TwinCollection.");
            }

            serializer.Serialize(writer, properties.JObject);
        }

        public override bool CanConvert(Type objectType) => typeof(TwinCollection).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new TwinCollection(JToken.ReadFrom(reader) as JObject);
        }
    }
}

