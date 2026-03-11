// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Optional payload including the DTDL model Id of the device.
    /// </summary>
    public class ModelIdPayload
    {
        /// <summary>
        /// The Id of the model the device adheres to for properties, telemetry, and commands.
        /// </summary>
        /// <remarks>
        /// For more information on device provisioning service and plug and play compatibility,
        /// For information on DTDL, see <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md"/>
        /// </remarks>
        [JsonProperty("modelId")]
        public string ModelId { get; set; }
    }
}
