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
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(OpenAsync)}");
                return this.EnsureOpenedAsync(true, cancellationToken);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(OpenAsync)}");
            }
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(ReceiveAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                return await base.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(ReceiveAsync)}");
            }
        }

        /// <summary>
        /// Receive a message from the device queue with the specified timeout
        /// </summary>
        /// <returns>The receive message or null if there was no message until the specified time has elapsed</returns>
        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeout, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(ReceiveAsync)}");

                TimeoutHelper.ThrowIfNegativeArgument(timeout);
                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                return await base.ReceiveAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeout, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(ReceiveAsync)}");
            }
        }

        public override async Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableMethodsAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.EnableMethodsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableMethodsAsync)}");
            }
        }

        public override async Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(DisableMethodsAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.DisableMethodsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(DisableMethodsAsync)}");
            }
        }

        public override async Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableEventReceiveAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.EnableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableEventReceiveAsync)}");
            }
        }

        public override async Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(DisableEventReceiveAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.DisableEventReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(DisableEventReceiveAsync)}");
            }
        }

        public override async Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableTwinPatchAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.EnableTwinPatchAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnableTwinPatchAsync)}");
            }
        }
        
        public override async Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendTwinGetAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                return await base.SendTwinGetAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendTwinGetAsync)}");
            }
        }
        
        public override async Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, reportedProperties, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendTwinPatchAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.SendTwinPatchAsync(reportedProperties, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, reportedProperties, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendTwinPatchAsync)}");
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue
        /// </summary>
        /// <returns>The lock identifier for the previously received message</returns>
        public override async Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(CompleteAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.CompleteAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(CompleteAsync)}");
            }
        }

        /// <summary>
        /// Puts a received message back onto the device queue
        /// </summary>
        /// <returns>The previously received message</returns>
        public override async Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(AbandonAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.AbandonAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(AbandonAsync)}");
            }
        }

        /// <summary>
        /// Deletes a received message from the device queue and indicates to the server that the message could not be processed.
        /// </summary>
        /// <returns>The previously received message</returns>
        public override async Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(RejectAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.RejectAsync(lockToken, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, lockToken, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(RejectAsync)}");
            }
        }

        /// <summary>
        /// Sends an event to device hub
        /// </summary>
        /// <returns>The message containing the event</returns>
        public override async Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, message, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendEventAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.SendEventAsync(message, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, message, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendEventAsync)}");
            }
        }

        public override async Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, methodResponse, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendMethodResponseAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.SendMethodResponseAsync(methodResponse, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, methodResponse, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendMethodResponseAsync)}");
            }
        }

        /// <summary>
        /// Sends a batch of events to device hub
        /// </summary>
        /// <returns>The task containing the event</returns>
        public override async Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, messages, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendEventAsync)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.SendEventAsync(messages, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, messages, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(SendEventAsync)}");
            }
        }

        /// <summary>
        /// Close the DeviceClient instance
        /// </summary>
        /// <returns></returns>
        public override async Task CloseAsync()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(CloseAsync)}");

                if (this.TryCloseGate())
                {
                    await base.CloseAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(CloseAsync)}");
            }
        }

        public override async Task RecoverConnections(object o, ConnectionType connectionType, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, o, connectionType, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(RecoverConnections)}");

                await this.EnsureOpenedAsync(false, cancellationToken).ConfigureAwait(false);
                await base.RecoverConnections(o, connectionType, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, o, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(RecoverConnections)}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(Dispose)}");

            this.TryCloseGate();
            base.Dispose(disposing);

            if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(Dispose)}");
        }

        bool TryCloseGate()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, this.open, this.closed, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(TryCloseGate)}");

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
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(TryCloseGate)}");
            }
        }

        Task EnsureOpenedAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            try
            {
            
                if (Logging.IsEnabled) Logging.Enter(this, explicitOpen, cancellationToken, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnsureOpenedAsync)}");

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
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(GateKeeperDelegatingHandler)}.{nameof(EnsureOpenedAsync)}");
            }
        }
    }
}
