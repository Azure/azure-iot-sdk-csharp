// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// An optional, helper class for constructing a writable property payload.
    /// </summary>
    /// <remarks>
    /// This is used by <see cref="WritableClientProperty.CreateAcknowledgement(int, string)"/> to create a writable property acknowledgement payload.
    /// This helper class will only work with <see cref="Newtonsoft.Json"/>.
    /// It uses <see cref="Newtonsoft.Json"/> based <see cref="JsonPropertyAttribute"/> to define the JSON property names.
    /// </remarks>
    public sealed class NewtonsoftJsonWritablePropertyAcknowledgementPayload : IWritablePropertyAcknowledgementPayload
    {
        /// <summary>
        /// Convenience constructor for specifying the properties.
        /// </summary>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgment version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgment description, an optional, human-readable message about the result of the property update.</param>
        public NewtonsoftJsonWritablePropertyAcknowledgementPayload(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        /// <summary>
        /// The unserialized property value.
        /// </summary>
        [JsonProperty(ConventionBasedConstants.ValuePropertyName)]
        public object Value { get; set; }

        /// <summary>
        /// The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        /// <remarks>
        /// Some commonly used codes are defined in <see cref="CommonClientResponseCodes" />.
        /// </remarks>
        [JsonProperty(ConventionBasedConstants.AckCodePropertyName)]
        public int AckCode { get; set; }

        /// <summary>
        /// The acknowledgment version, as supplied in the property update request.
        /// </summary>
        [JsonProperty(ConventionBasedConstants.AckVersionPropertyName)]
        public long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgment description, an optional, human-readable message about the result of the property update.
        /// </summary>
        [JsonProperty(ConventionBasedConstants.AckDescriptionPropertyName, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AckDescription { get; set; }
    }
}
