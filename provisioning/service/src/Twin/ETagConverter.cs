// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ETagConverter : JsonConverter<ETag>
    {
        public override ETag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString();
            if (value == null)
            {
                return default;
            }

            return new ETag(value);
        }

        public override void Write(Utf8JsonWriter writer, ETag value, JsonSerializerOptions options)
        {
            if (value == default)
            {
                writer.WriteNullValue();
            }
            else
            {
                // This is the one line that is different from the Azure.Core implementation.
                // This is done because the ETag in this package is serialized for the payload
                // object rather than the header object (which is more common)
                writer.WriteStringValue(value.ToString("G"));
            }
        }
    }
}