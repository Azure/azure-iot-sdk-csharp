// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal abstract class DefaultDelegatingHandler : IDelegatingHandler
    {
        private volatile IDelegatingHandler _innerHandler;
        protected volatile bool _disposed;

        protected DefaultDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler)
        {
            Context = context;
            _innerHandler = innerHandler;
            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _innerHandler, nameof(InnerHandler));
            }
        }

        public IPipelineContext Context { get; protected set; }

        public ContinuationFactory<IDelegatingHandler> ContinuationFactory { get; set; }

        public IDelegatingHandler InnerHandler
        {
            get => _innerHandler;
            protected set
            {
                _innerHandler = value;
                if (Logging.IsEnabled)
                {
                    Logging.Associate(this, _innerHandler, nameof(InnerHandler));
                }
            }
        }

        public virtual Task OpenAsync(TimeoutHelper timeoutHelper)
        {
            ThrowIfDisposed();
            return InnerHandler?.OpenAsync(timeoutHelper) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task OpenAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.OpenAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task CloseAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (InnerHandler == null)
            {
                return TaskHelpers.CompletedTask;
            }
            else
            {
                Task closeTask = InnerHandler.CloseAsync(cancellationToken);
                return closeTask;
            }
        }

        /// <summary>
        /// Completes when the transport disconnected.
        /// </summary>
        public virtual Task WaitForTransportClosedAsync()
        {
            ThrowIfDisposed();

            if (InnerHandler == null)
            {
                throw new InvalidOperationException();
            }

            return InnerHandler.WaitForTransportClosedAsync();
        }

        public virtual Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler.ReceiveAsync(cancellationToken);
        }

        public virtual Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            ThrowIfDisposed();
            return InnerHandler.ReceiveAsync(timeoutHelper);
        }

        public virtual Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler.EnableReceiveMessageAsync(cancellationToken);
        }

        // This is to ensure that if device connects over MQTT with CleanSession flag set to false,
        // then any message sent while the device was disconnected is delivered on the callback.
        public virtual Task EnsurePendingMessagesAreDeliveredAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler.EnsurePendingMessagesAreDeliveredAsync(cancellationToken);
        }

        public virtual Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler.DisableReceiveMessageAsync(cancellationToken);
        }

        public virtual Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.CompleteAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.AbandonAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.RejectAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.SendEventAsync(message, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.SendEventAsync(messages, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.EnableMethodsAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.DisableMethodsAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.SendMethodResponseAsync(methodResponse, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.EnableTwinPatchAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.DisableTwinPatchAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.SendTwinGetAsync(cancellationToken) ?? Task.FromResult((Twin)null);
        }

        public virtual Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.SendTwinPatchAsync(reportedProperties, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual bool IsUsable => InnerHandler?.IsUsable ?? true;

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("IoT Client");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _innerHandler?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DefaultDelegatingHandler()
        {
            Dispose(false);
        }
    }
}
