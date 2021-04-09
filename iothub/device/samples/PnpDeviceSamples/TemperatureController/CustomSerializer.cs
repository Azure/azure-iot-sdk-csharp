// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class CustomSerializer
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        internal static string SerializeToString(object objectToSerialize)
        {
            return JsonSerializer.Serialize(objectToSerialize, s_options);
        }

        internal static T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonSerializer.Deserialize<T>(stringToDeserialize);
        }
    }
}
