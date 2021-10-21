// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains methods that services can use to perform receive operations.
    /// </summary>
    public abstract class Receiver<T>
    {
        /// <summary>
        /// Receive a message using the default timeout.
        /// </summary>
        /// <returns>The receive message or null if there was no message until the default timeout.</returns>
        [Obsolete("Deprecated in favor of method signature with CancellationToken.")]
        public abstract Task<T> ReceiveAsync();

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="timeout">The timeout for receiving a message.</param>
        /// <returns>The receive message or null if there was no message until the specified timeout.</returns>
        [Obsolete("Deprecated in favor of method signature with CancellationToken.")]
        public abstract Task<T> ReceiveAsync(TimeSpan timeout);

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="cancellationToken">The Cancellation token.</param>
        /// <returns>The receive message or null if there was no message until the specified timeout.</returns>
        public abstract Task<T> ReceiveAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a received message from the queue.
        /// </summary>
        [Obsolete("Deprecated in favor of method signature with CancellationToken.")]
        public abstract Task CompleteAsync(T t);

        /// <summary>
        /// Deletes a received message from the queue.
        /// </summary>
        /// <param name="t">The message to be deleted.</param>
        /// <param name="cancellationToken">The Cancellation token.</param>
        public abstract Task CompleteAsync(T t, CancellationToken cancellationToken);

        /// <summary>
        /// Puts a received message back into the queue.
        /// </summary>
        [Obsolete("Deprecated in favor of method signature with CancellationToken.")]
        public abstract Task AbandonAsync(T t);

        /// <summary>
        /// Puts a received message back into the queue.
        /// </summary>
        /// <param name="t">The message to be abandoned.</param>
        /// <param name="cancellationToken">The Cancellation token.</param>
        public abstract Task AbandonAsync(T t, CancellationToken cancellationToken);
    }
}
