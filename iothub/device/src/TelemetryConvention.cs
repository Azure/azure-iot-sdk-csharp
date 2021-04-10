// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class TelemetryConvention : ObjectSerializer
    {
        /// <summary>
        /// The content type for a plug and play compatible telemetry message.
        /// </summary>
        private const string ApplicationJson = "application/json";

        /// <summary>
        ///
        /// </summary>
        public static new readonly TelemetryConvention Instance = new TelemetryConvention();

        /// <summary>
        ///
        /// </summary>
        public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        ///
        /// </summary>
        public string ContentType { get; set; } = ApplicationJson;

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

        /// <summary>
        ///
        /// </summary>
        /// <param name="contentPayload"></param>
        /// <returns></returns>
        public virtual byte[] EncodeStringToByteArray(string contentPayload)
        {
            return ContentEncoding.GetBytes(contentPayload);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSendWithConvention"></param>
        /// <returns></returns>
        public virtual byte[] GetObjectBytes(object objectToSendWithConvention)
        {
            return EncodeStringToByteArray(SerializeToString(objectToSendWithConvention));
        }
    }
}
