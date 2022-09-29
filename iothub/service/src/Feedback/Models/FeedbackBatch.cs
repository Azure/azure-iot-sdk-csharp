// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains a batch of feedback records.
    /// </summary>
    /// <remarks>
    /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT hub.
    /// </remarks>
    public class FeedbackBatch
    {
        /// <summary>
        /// This constructor is for deserialization and unit test mocking purposes.
        /// </summary>
        /// <remarks>
        /// To unit test methods that use this type as a response, inherit from this class and give it a constructor
        /// that can set the properties you want.
        /// </remarks>
        protected internal FeedbackBatch()
        { }

        /// <summary>
        /// When the feedback message was received by the IoT hub in UTC.
        /// </summary>
        /// <remarks>
        /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT hub.
        /// </remarks>
        public DateTime EnqueuedOnUtc { get; protected internal set; }

        /// <summary>
        /// A collection of feedback records of C2D messages across multiple devices in the IoT hub.
        /// </summary>
        /// <remarks>
        /// Feedback messages are delivery acknowledgments for messages sent to a device from IoT hub.
        /// </remarks>
        public IEnumerable<FeedbackRecord> Records { get; protected internal set; }

        /// <summary>
        /// The IoT hub host name.
        /// </summary>
        public string IotHubHostName { get; protected internal set; }
    }
}
