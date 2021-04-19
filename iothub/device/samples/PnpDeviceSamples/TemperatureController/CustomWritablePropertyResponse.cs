// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class CustomWritablePropertyResponse : WritablePropertyBase
    {
        internal CustomWritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
            : base(propertyValue, ackCode, ackVersion, ackDescription)
        {
        }

        [JsonPropertyName(ValuePropertyName)]
        public override object Value { get; set; }

        [JsonPropertyName(AckCodePropertyName)]
        public override int AckCode { get; set; }

        [JsonPropertyName(AckVersionPropertyName)]
        public override long AckVersion { get; set; }

        [JsonPropertyName(AckDescriptionPropertyName)]
        public override string AckDescription { get; set; }
    }
}
