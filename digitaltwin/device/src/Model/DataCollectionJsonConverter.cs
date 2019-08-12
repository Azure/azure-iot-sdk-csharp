// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "CodeAnalysis limitation: TwinCollectionJsonConverter is actually used by TwinCollection")]
    internal class DataCollectionJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            DataCollection properties = value as DataCollection;
            if (properties == null)
            {
                throw new InvalidOperationException("Object passed is not of type DataCollection.");
            }

            serializer.Serialize(writer, properties.JObject);
        }

        public override bool CanConvert(Type objectType) => typeof(DataCollection).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            reader.DateParseHandling = DateParseHandling.None;
            return new DataCollection(JToken.ReadFrom(reader).ToString());
        }
    }
}
