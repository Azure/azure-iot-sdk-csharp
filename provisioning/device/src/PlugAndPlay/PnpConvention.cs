// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Extensions;

namespace Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay
{
    /// <summary>
    /// A helper class for formatting the DPS device registration payload, per plug and play convention.
    /// </summary>
    public static class PnpConvention
    {
        /// <summary>
        /// Create the DPS payload to provision a device as plug and play.
        /// </summary>
        /// <remarks>
        /// For more information on device provisioning service and plug and play compatibility,
        /// and PnP device certification, see <see href="https://docs.microsoft.com/azure/iot-pnp/howto-certify-device"/>.
        /// The DPS payload should be in the format:
        /// <c>
        /// {
        ///   "modelId": "dtmi:com:example:modelName;1"
        /// }
        /// </c>
        /// For information on DTDL, see <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md"/>
        /// </remarks>
        /// <param name="modelId">The Id of the model the device adheres to for properties, telemetry, and commands.</param>
        /// <returns>The DPS payload to provision a device as plug and play.</returns>
        public static string CreateDpsPayload(string modelId)
        {
            modelId.ThrowIfNullOrWhiteSpace(nameof(modelId));
            return $"{{\"modelId\":\"{modelId}\"}}";
        }
    }
}
