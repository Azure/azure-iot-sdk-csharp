// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Type that is used to deserialize and represent the received client properties.
    /// This class uses System.Text.Json for the top-level property deserialization
    /// since the property names are known and defined by service contract.
    /// </summary>
    internal class TwinDocument
    {
        [JsonPropertyName("desired")]
        internal Dictionary<string, object> Desired { get; set; }

        [JsonPropertyName("reported")]
        internal Dictionary<string, object> Reported { get; set; }
    }
}
