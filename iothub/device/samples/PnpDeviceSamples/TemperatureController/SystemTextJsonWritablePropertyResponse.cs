// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class SystemTextJsonWritablePropertyResponse : IWritablePropertyResponse
    {
        internal SystemTextJsonWritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        [JsonPropertyName(ConventionBasedConstants.ValuePropertyName)]
        public object Value { get; set; }

        [JsonPropertyName(ConventionBasedConstants.AckCodePropertyName)]
        public int AckCode { get; set; }

        [JsonPropertyName(ConventionBasedConstants.AckVersionPropertyName)]
        public long AckVersion { get; set; }

        [JsonPropertyName(ConventionBasedConstants.AckDescriptionPropertyName)]
        public string AckDescription { get; set; }
    }
}
