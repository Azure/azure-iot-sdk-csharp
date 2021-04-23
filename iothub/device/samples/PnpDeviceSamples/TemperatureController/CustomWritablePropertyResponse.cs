// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class CustomWritablePropertyResponse
    {
        internal CustomWritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        [JsonPropertyName(WritablePropertyResponse.ValuePropertyName)]
        public object Value { get; set; }

        [JsonPropertyName(WritablePropertyResponse.AckCodePropertyName)]
        public int AckCode { get; set; }

        [JsonPropertyName(WritablePropertyResponse.AckVersionPropertyName)]
        public long AckVersion { get; set; }

        [JsonPropertyName(WritablePropertyResponse.AckDescriptionPropertyName)]
        public string AckDescription { get; set; }
    }
}
