// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Handles a specific type of service response that looks like this:
    /// <code>
    /// {
    ///   "Message": "ErrorCode:DeviceNotFound;E2E_MessageReceiveE2EPoolAmqpTests__3_Sasl_f16d18b2-97dc-4ea5-86f1-a3405ee98939",
    ///   "ExceptionMessage":"Tracking ID:aeec4c1e4e914a4c9f40fdba7be68fa5-G:0-TimeStamp:10/18/2022 20:50:39"
    /// }
    /// </code>
    /// </summary>
    internal class ResponseMessage2
    {
        [JsonPropertyName("Message")]
        public string Message { get; set; }

        [JsonPropertyName("ExceptionMessage")]
        public string ExceptionMessage { get; set; }

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
                if (part.StartsWith(errorCodeLabel))
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

            if (!ExceptionMessage.StartsWith(trackingIdLabel))
            {
                return false;
            }

            TrackingId = ExceptionMessage.Substring(trackingIdLabel.Length);

            return true;
        }
    }
}
