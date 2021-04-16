// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The default implementation of the <see cref="WritablePropertyBase"/> that uses <see cref="JsonPropertyAttribute"/> to define the JSON property names.
    /// </summary>
    public sealed class WritablePropertyResponse : WritablePropertyBase
    {
        /// <inheritdoc />
        public WritablePropertyResponse(object propertyValue) : base(propertyValue)
        {
        }

        /// <inheritdoc />
        public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)            
            : base(propertyValue, ackCode, ackVersion, ackDescription)
        {
        }

        /// <inheritdoc />
        [JsonProperty(ValuePropertyName)]
        public override object Value { get; set; }

        /// <inheritdoc />
        [JsonProperty(AckCodePropertyName)]
        public override int AckCode { get; set; }

        /// <inheritdoc />
        [JsonProperty(AckVersionPropertyName)]
        public override long AckVersion { get; set; }

        /// <inheritdoc />
        [JsonProperty(AckDescriptionPropertyName, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public override string AckDescription { get; set; }
    }
}
