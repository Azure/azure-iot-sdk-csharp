// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public static class TelemetryConventionHelper
    {
        /// <summary>
        /// Format a plug and play compatible telemetry message payload.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="telemetryValue">The unserialized telemetry payload, in the format defined in the DTDL interface.</param>
        /// <returns>A plug and play compatible telemetry message payload, which can be sent to IoT Hub.</returns>
        public static IDictionary<string, object> FormatTelemetryPayload(string telemetryName, object telemetryValue)
        {
            return new Dictionary<string, object> { { telemetryName, telemetryValue } };
        }
    }
}
