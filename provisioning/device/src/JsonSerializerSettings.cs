// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Client
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
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    IgnoreEmptyCollectionsAndUnsetEtags,
                }
            }
        };

        private static void IgnoreEmptyCollectionsAndUnsetEtags(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

            foreach (var prop in typeInfo.Properties)
            {
                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                {
                    // Only serialize a collection if it has at least one element
                    prop.ShouldSerialize = (_, value) =>
                    {
                        if (value == null)
                        {
                            return false;
                        }

                        return value is ICollection c ? c.Count > 0 : ((IEnumerable)value).GetEnumerator().MoveNext();
                    };
                }
            }
        }
    }
}
