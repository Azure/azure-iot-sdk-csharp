// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The possible acknowledgement types for a received file upload notification and/or for a received cloud-to-device feedback message.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AcknowledgementType
    {
        /// <summary>
        /// This acknowledgement will remove the received message from the service's message queue as it was received and handled by the client.
        /// </summary>
        Complete,

        /// <summary>
        /// This acknowledgement will requeue the received message back into the service's message queue so that it will be sent again.
        /// </summary>
        /// <remarks>
        /// This may be done if the message in question should be received by a different receiver, or at a later
        /// time. Each service message has a finite number of times that it can be received and then abandoned before the message
        /// will be removed from the queue. This maximum number of times it can be received can be set through the Azure portal.
        /// </remarks>
        Abandon
    }
}
