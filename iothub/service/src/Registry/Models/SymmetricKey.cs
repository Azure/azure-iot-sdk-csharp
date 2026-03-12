// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Primary and secondary symmetric keys of a device or module.
    /// </summary>
    public sealed class SymmetricKey
    {
        /// <summary>
        /// Gets or sets the primary key.
        /// </summary>
        [JsonPropertyName("primaryKey")]
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets the secondary key.
        /// </summary>
        [JsonPropertyName("secondaryKey")]
        public string SecondaryKey { get; set; }
    }
}
