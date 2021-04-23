// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// An optional, helper class for constructing a writable property response.
    /// </summary>
    /// <remarks>
    /// This helper class will only work with <see cref="Newtonsoft.Json"/>.
    /// It uses <see cref="Newtonsoft.Json"/> based <see cref="JsonPropertyAttribute"/> to define the JSON property names.
    /// For scenarios where you want to use a different serializer, you will need to implement one with its corresponding seriailizer attributes.
    /// </remarks>
    public sealed class WritablePropertyResponse
    {
        /// <summary>
        /// Represents the JSON document property name for the value
        /// </summary>
        public const string ValuePropertyName = "value";

        /// <summary>
        /// Represents the JSON document property name for the Ack Code
        /// </summary>
        public const string AckCodePropertyName = "ac";

        /// <summary>
        /// Represents the JSON document property name for the Ack Version
        /// </summary>
        public const string AckVersionPropertyName = "av";

        /// <summary>
        /// Represents the JSON document property name for the Ack Description
        /// </summary>
        public const string AckDescriptionPropertyName = "ad";

        /// <summary>
        /// Convenience constructor for specifying the properties.
        /// </summary>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        public WritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        /// <summary>
        /// The unserialized property value.
        /// </summary>
        [JsonProperty(ValuePropertyName)]
        public object Value { get; set; }

        /// <summary>
        /// The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        [JsonProperty(AckCodePropertyName)]
        public int AckCode { get; set; }

        /// <summary>
        /// The acknowledgement version, as supplied in the property update request.
        /// </summary>
        [JsonProperty(AckVersionPropertyName)]
        public long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgement description, an optional, human-readable message about the result of the property update.
        /// </summary>
        [JsonProperty(AckDescriptionPropertyName, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AckDescription { get; set; }
    }
}
