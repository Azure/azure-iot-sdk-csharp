// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System.Diagnostics;

    /// <summary>
    /// Contains the implementation of methods that a device can use to send messages to and receive from the service.
    /// </summary>
    sealed class GateKeeperDelegatingHandler : DefaultDelegatingHandler
    {
        bool open;
        bool closed;
        volatile TaskCompletionSource<object> openTaskCompletionSource;
        readonly object thisLock;

        public GateKeeperDelegatingHandler(IPipelineContext context)
            : base(context)
        {
            this.thisLock = new object();
            this.openTaskCompletionSource = new TaskCompletionSource<object>(this);
        }

        /// <summary>
        /// Explicitly open the DeviceClient instance.
        /// </summary>
        public Task OpenAsync(CancellationToken cancellationToken)
        {
            Debug.WriteLine(cancellationToken.GetHashCode() + " GateKeeperDelegatingHandler.OpenAsync()");
            return this.EnsureOpenedAsync(true, cancellationToken);
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            TimeoutHelper.ThrowIfNegativeArgument(timeout);
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            return await base.ReceiveAsync(timeout, cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.DisableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
        }
        
        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            return await base.SendTwinGetAsync(cancellationToken).ConfigureAwait(false);
        }
        
        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties,  CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.SendTwinPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.CompleteAsync(lockToken, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.AbandonAsync(lockToken, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.RejectAsync(lockToken, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.SendMethodResponseAsync(methodResponse, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            Debug.WriteLine(cancellationToken.GetHashCode() + " GateKeeperDelegatingHandler.SendEventAsync() ENTER");
            try
            {
                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Debug.WriteLine(cancellationToken.GetHashCode() + " GateKeeperDelegatingHandler.SendEventAsync() EXIT");
            }
        }

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        public override async Task CloseAsync()
        {
            if (this.TryCloseGate())
            {
                Debug.WriteLine("GateKeeperDelegatingHandler.CloseAsync()");
                await base.CloseAsync().ConfigureAwait(false);
            }
        }

        public override async Task RecoverConnections(object o, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
            await base.RecoverConnections(o, connectionType, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            this.TryCloseGate();
            base.Dispose(disposing);
        }

        bool TryCloseGate()
        {
            TaskCompletionSource<object> localOpenTaskCompletionSource;
            lock (this.thisLock)
            {
                if (this.closed)
                {
                    return false;
                }

                localOpenTaskCompletionSource = this.openTaskCompletionSource;
                this.closed = true;
            }

            localOpenTaskCompletionSource?.TrySetCanceled();
            return this.open;
        }

        Task EnsureOpenedAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            lock (this.thisLock)
            {
                if (this.closed)
                {
                    throw new ObjectDisposedException("The object has been closed and cannot be reopened.");
                }
            }

            bool needOpen = false;
            Task openTask;
            if (this.openTaskCompletionSource != null)
            {
                lock (this.thisLock)
                {
                    if (this.open)
                    {
                        if (this.openTaskCompletionSource == null)
                        {
                            // openTaskCompletionSource being null means open has finished completely
                            openTask = TaskHelpers.CompletedTask;
                        }
                        else
                        {
                            openTask = this.openTaskCompletionSource.Task;
                        }
                    }
                    else
                    {
                        // It's this call's job to kick off the open.
                        this.open = true;
                        openTask = this.openTaskCompletionSource.Task;
                        needOpen = true;
                    }
                }
            }
            else
            {
                // Open has already fully completed.
                openTask = TaskHelpers.CompletedTask;
            }

            if (needOpen)
            {
                try
                {
                    base.OpenAsync(explicitOpen, cancellationToken).ContinueWith(
                        t =>
                        {
                            TaskCompletionSource<object> localOpenTaskCompletionSource;
                            lock (this.thisLock)
                            {
                                localOpenTaskCompletionSource = this.openTaskCompletionSource;
                                if (!t.IsFaulted && !t.IsCanceled)
                                {
                                    // This lets future calls avoid the Open logic all together.
                                    this.openTaskCompletionSource = null;
                                }
                                else
                                {
                                    // OpenAsync was canceled or threw an exception, next time retry.
                                    this.open = false;
                                    this.openTaskCompletionSource = new TaskCompletionSource<object>(this);
                                }
                            }

                            // This completes anyone waiting for open to finish
                            TaskHelpers.MarshalTaskResults(t, localOpenTaskCompletionSource);
                        });
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    TaskCompletionSource<object> localOpenTaskCompletionSource;
                    lock (this.thisLock)
                    {
                        localOpenTaskCompletionSource = this.openTaskCompletionSource;
                        this.open = false;
                        this.openTaskCompletionSource = new TaskCompletionSource<object>(this);
                    }
                    localOpenTaskCompletionSource.SetException(ex);

                    if (ex is TaskCanceledException)
                    {
                        throw new TimeoutException();
                    }

                    throw;
                }
            }

            return openTask;
        }
    }
}
