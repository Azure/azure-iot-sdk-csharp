// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class used as a model to deserialize one schema type of errors received from IoT hub.
    /// </summary>
    /// <remarks>
    /// Handles a specific type of service response that looks like this:
    /// <code>
    /// {
    ///   "Message": "ErrorCode:DeviceNotFound;E2E_MessageReceiveE2EPoolAmqpTests__3_Sasl_f16d18b2-97dc-4ea5-86f1-a3405ee98939",
    ///   "ExceptionMessage":"Tracking ID:aeec4c1e4e914a4c9f40fdba7be68fa5-G:0-TimeStamp:10/18/2022 20:50:39"
    /// }
    /// </code>
    /// </remarks>
    internal sealed class ErrorPayload2
    {
        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'Message'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonProperty("Message")]
        internal string Message { get; set; }

        [SuppressMessage("Usage", "CA1507: Use nameof in place of string literal 'ExceptionMessage'",
            Justification = "This JsonProperty annotation depends on service-defined contract (name) and is independent of the property name selected by the SDK.")]
        [JsonProperty("ExceptionMessage")]
#pragma warning restore CA1507 // Use nameof in place of string
        internal string ExceptionMessage { get; set; }

        public IotHubServiceErrorCode ErrorCode { get; set; }
        public string TrackingId { get; set; }

        internal bool TryParse()
        {
            if (string.IsNullOrWhiteSpace(Message)
                || string.IsNullOrWhiteSpace(ExceptionMessage))
            {
                return false;
            }

            const string errorCodeLabel = "ErrorCode";

            string[] messageParts = Message.Split(';');

            foreach (string part in messageParts)
            {
                if (part.StartsWith(errorCodeLabel, StringComparison.InvariantCulture))
                {
                    string[] errorCodeParts = part.Split(':');

                    if (errorCodeParts.Length != 2)
                    {
                        return false;
                    }

                    if (!Enum.TryParse(errorCodeParts[1], out IotHubServiceErrorCode errorCode))
                    {
                        return false;
                    }
                    ErrorCode = errorCode;
                    break;
                }
            }

            const string trackingIdLabel = "Tracking ID:";

            if (!ExceptionMessage.StartsWith(trackingIdLabel, StringComparison.InvariantCulture))
            {
                return false;
            }

            TrackingId = ExceptionMessage.Substring(trackingIdLabel.Length);

            return true;
        }
    }
}
