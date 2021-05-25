// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// An optional, helper class for constructing a writable property response.
    /// </summary>
    /// <remarks>
    /// This helper class will only work with <see cref="System.Text.Json"/>.
    /// It uses <see cref="System.Text.Json"/> based <see cref="JsonPropertyNameAttribute"/> to define the JSON property names.
    /// </remarks>
    public sealed class SystemTextJsonWritablePropertyResponse : IWritablePropertyResponse
    {
        /// <summary>
        /// Convenience constructor for specifying the properties.
        /// </summary>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgment version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgment description, an optional, human-readable message about the result of the property update.</param>
        public SystemTextJsonWritablePropertyResponse(object propertyValue, int ackCode, long ackVersion, string ackDescription = default)
        {
            Value = propertyValue;
            AckCode = ackCode;
            AckVersion = ackVersion;
            AckDescription = ackDescription;
        }

        /// <summary>
        /// The unserialized property value.
        /// </summary>
        [JsonPropertyName(ConventionBasedConstants.ValuePropertyName)]
        public object Value { get; set; }

        /// <summary>
        /// The acknowledgment code, usually an HTTP Status Code e.g. 200, 400.
        /// </summary>
        [JsonPropertyName(ConventionBasedConstants.AckCodePropertyName)]
        public int AckCode { get; set; }

        /// <summary>
        /// The acknowledgment version, as supplied in the property update request.
        /// </summary>
        [JsonPropertyName(ConventionBasedConstants.AckVersionPropertyName)]
        public long AckVersion { get; set; }

        /// <summary>
        /// The acknowledgment description, an optional, human-readable message about the result of the property update.
        /// </summary>
        [JsonPropertyName(ConventionBasedConstants.AckDescriptionPropertyName)]
        public string AckDescription { get; set; }
    }
}
