// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Utilities
{
    /// <summary>
    /// The System.Text.Json serialization settings that this library will use when serializing/deserializing. May be updated.
    /// </summary>
    public class JsonSerializerSettings
    {
        /// <summary>
        /// The System.Text.Json serialization settings that this library will use when serializing/deserializing. May be updated.
        /// </summary>
        public static JsonSerializerOptions Options { get; set; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
