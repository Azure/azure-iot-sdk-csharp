// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    /// <summary>
    /// A class for holding the task completion source of a pending operation and the date/time when that operation
    /// was initiated. Because PubSub behavior has us send a request on one sending link and receive a response later on another
    /// receiving link, we need a way to identify the request, when it was sent, and the task completion source to complete the
    /// waiting user's task.
    /// </summary>
    internal sealed class PendingAmqpTwinOperation
    {
        public PendingAmqpTwinOperation(TaskCompletionSource<AmqpMessage> completionTask)
        {
            CompletionTask = completionTask;
        }

        /// <summary>
        /// The pending task to be signaled when complete.
        /// </summary>
        public TaskCompletionSource<AmqpMessage> CompletionTask { get; }

        /// <summary>
        /// When the request was sent so we know when to time out older operations
        /// </summary>
        public DateTimeOffset RequestSentOnUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
