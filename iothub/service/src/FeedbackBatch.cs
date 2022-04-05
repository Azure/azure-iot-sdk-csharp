﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains a batch of feedback records.
    /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT Hub.
    /// </summary>
    public class FeedbackBatch
    {
        /// <summary>
        /// Date and time that indicates when the feedback message was received by the IoT hub.
        /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT Hub.
        /// </summary>
        public DateTime EnqueuedTime { get; set; }

        /// <summary>
        /// A collection of feedback records of C2D messages across multiple devices in the IoT hub.
        /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT Hub.
        /// </summary>
        public IEnumerable<FeedbackRecord> Records { get; set; }

        /// <summary>
        /// The IoT hub hostname.
        /// </summary>
        /// <remarks>
        /// Despite the name of this member, the value will be the IoT hub hostname.
        /// </remarks>
        public string UserId { get; set; }

        internal string LockToken { get; set; }
    }
}
