// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Represents the device method invocation results.
    /// </summary>
    public class DigitalTwinCommandResponse
    {
        /// <summary>
        /// Command invocation result status, as supplied by the device.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Method invocation result payload, as supplied by the device.
        /// </summary>
        public string Payload { get; set; }
    }
}
