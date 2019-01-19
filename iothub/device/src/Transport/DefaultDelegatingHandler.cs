// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Common;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal abstract class DefaultDelegatingHandler : IDelegatingHandler
    {
        private static readonly Task<Message> s_dummyResultObject = Task.FromResult((Message)null);
        private IDelegatingHandler _innerHandler;
        protected bool _disposed;

        protected DefaultDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler)
        {
            Context = context;
            _innerHandler = innerHandler;
            if (Logging.IsEnabled) Logging.Associate(this, _innerHandler, nameof(InnerHandler));
        }

        public IPipelineContext Context { get; protected set; }

        public ContinuationFactory<IDelegatingHandler> ContinuationFactory { get; set; }

        public IDelegatingHandler InnerHandler
        {
            get
            {
                return _innerHandler;
            }
            protected set
            {
                if (Logging.IsEnabled) Logging.Associate(this, _innerHandler, nameof(InnerHandler));
                _innerHandler = value;
            }
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
        /// <returns></returns>
        public virtual Task WaitForTransportClosedAsync()
        {
            ThrowIfDisposed();

            if (InnerHandler == null) throw new InvalidOperationException();
            return InnerHandler.WaitForTransportClosedAsync();
        }

        public virtual Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.ReceiveAsync(cancellationToken) ?? s_dummyResultObject;
        }

        public virtual Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.ReceiveAsync(timeout, cancellationToken) ?? s_dummyResultObject;
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

        public virtual Task EnableEventReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.EnableEventReceiveAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableEventReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return InnerHandler?.DisableEventReceiveAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException("IoT Client");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

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
