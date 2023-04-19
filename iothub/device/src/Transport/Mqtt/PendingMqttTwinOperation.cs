// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// A class for holding the task completion source of a pending operation and the date/time when that operation
    /// was initiated. Because PubSub behavior has us send a request on one topic and receive a response later on another
    /// topic, we need a way to identify the request, when it was sent, and the task completion source to complete the
    /// waiting user's task.
    /// </summary>
    internal class PendingMqttTwinOperation
    {
        /// <summary>
        /// Constructor for get twin operations.
        /// </summary>
        public PendingMqttTwinOperation(TaskCompletionSource<GetTwinResponse> twinResponseTask)
        {
            TwinResponseTask = twinResponseTask;
        }

        /// <summary>
        /// Constructor for patch twin operations.
        /// </summary>
        public PendingMqttTwinOperation(TaskCompletionSource<PatchTwinResponse> twinPatchTask)
        {
            TwinPatchTask = twinPatchTask;
        }

        /// <summary>
        /// The pending task for get twin to be signaled when complete.
        /// </summary>
        /// <remarks>
        /// Will be null if this if this class is not being used for get twin.
        /// </remarks>
        public TaskCompletionSource<GetTwinResponse> TwinResponseTask { get; }

        /// <summary>
        /// The pending task for patching a twin to be signaled when complete.
        /// </summary>
        /// <remarks>
        /// Will be null if this if this class is not being used for patch twin.
        /// </remarks>
        public TaskCompletionSource<PatchTwinResponse> TwinPatchTask { get; }

        /// <summary>
        /// When the request was sent so we know when to time out older operations
        /// </summary>
        public DateTimeOffset RequestSentOnUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
