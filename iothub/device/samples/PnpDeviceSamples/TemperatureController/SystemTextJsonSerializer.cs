﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class SystemTextJsonSerializer : PayloadSerializer
    {
        private const string ApplicationJson = "application/json";

        private static readonly JsonSerializerOptions s_options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        public override string ContentType => ApplicationJson;

        public override string SerializeToString(object objectToSerialize)
        {
            return JsonSerializer.Serialize(objectToSerialize, s_options);
        }

        public override T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonSerializer.Deserialize<T>(stringToDeserialize, s_options);
        }

        public override T ConvertFromObject<T>(object objectToConvert)
        {
            return DeserializeToType<T>(((JsonElement)objectToConvert).ToString());
        }
        
        public override bool TryGetNestedObjectValue<T>(object objectToConvert, string propertyName, out T outValue)
        {
            outValue = default;
            if (objectToConvert == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }
            if (((JsonElement)objectToConvert).TryGetProperty(propertyName, out var element)) {
                outValue = DeserializeToType<T>(element.GetRawText());
                return true;
            }
            return false;
        }

        public override IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = null)
        {
            return new SystemTextJsonWritablePropertyResponse(value, statusCode, version, description);
        }
    }
}
