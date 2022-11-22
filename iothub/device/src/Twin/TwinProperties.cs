// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for client properties retrieved from the service.
    /// </summary>
    public class TwinProperties
    {
        /// <summary>
        /// The collection of desired property update requests received from service.
        /// </summary>
        [JsonPropertyName("desired")]
        public DesiredProperties Desired { get; protected internal set; } = new();

        /// <summary>
        /// The collection of twin properties reported by the client.
        /// </summary>
        [JsonPropertyName("reported")]
        public ReportedProperties Reported { get; protected internal set; } = new();
    }
}
