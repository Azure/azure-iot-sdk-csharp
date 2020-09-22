// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Extensions;

namespace Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlayConvention
{
    /// <summary>
    /// A helper class for formatting the data as per plug and play convention.
    /// </summary>
    public static class PnpConventionHelper
    {
        /// <summary>
        /// Create the DPS payload to provision a device as plug and play.
        /// For more information, see https://docs.microsoft.com/en-us/azure/iot-pnp/howto-certify-device.
        /// </summary>
        /// <remarks>
        /// The DPS payload should be in the format:
        ///     {
        ///         "modelId": "valid-dtdl-model-id"
        ///     }
        /// </remarks>
        /// <param name="modelId">The Id of the model the device adheres to for properties, telemetry, and commands.</param>
        /// <returns>The DPS payload to provision a device as plug and play.</returns>
        public static string CreateDpsPayload(string modelId)
        {
            modelId.ThrowIfNullOrWhiteSpace(nameof(modelId));
            return $"{{ \"modelId\": \"{modelId}\" }}".TrimWhiteSpace();
        }
    }
}
