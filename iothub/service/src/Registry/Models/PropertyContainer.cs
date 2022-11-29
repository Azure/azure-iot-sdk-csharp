// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The desired and reported properties of the twin.
    /// </summary>
    /// <remarks>
    /// Type definition for the <see cref="ExportImportDevice.Properties"/> property.
    /// The maximum depth of the object is 10.
    /// </remarks>
    public sealed class PropertyContainer
    {
        /// <summary>
        /// The collection of desired property key-value pairs.
        /// </summary>
        /// <remarks>
        /// The keys are UTF-8 encoded, case-sensitive and up-to 1KB in length. Allowed characters
        /// exclude UNICODE control characters (segments C0 and C1), '.', '$' and space. The
        /// desired porperty values are JSON objects, up-to 4KB in length.
        /// </remarks>
        [JsonPropertyName("desired")]
        public ClientTwinPropertyCollection DesiredProperties { get; set; }

        /// <summary>
        /// The collection of reported property key-value pairs.
        /// </summary>
        /// <remarks>
        /// The keys are UTF-8 encoded, case-sensitive and up-to 1KB in length. Allowed characters
        /// exclude UNICODE control characters (segments C0 and C1), '.', '$' and space. The
        /// reported property values are JSON objects, up-to 4KB in length.
        /// </remarks>
        [JsonPropertyName("reported")]
        public ClientTwinPropertyCollection ReportedProperties { get; set; }
    }
}
