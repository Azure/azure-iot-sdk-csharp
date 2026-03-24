// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Type that is used to deserialize and represent the received client properties.
    /// This class uses System.Text.Json for the top-level property deserialization
    /// since the property names are known and defined by service contract.
    /// </summary>
    public sealed class TwinDocument
    {
        /// <summary>
        /// The desired properties for this device
        /// </summary>
        [JsonPropertyName("desired")]
        public Dictionary<string, object> Desired { get; set; }

        /// <summary>
        /// The reported properties for this device
        /// </summary>
        [JsonPropertyName("reported")]
        public Dictionary<string, object> Reported { get; set; }
    }
}
