// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal abstract class DefaultDelegatingHandler : IDelegatingHandler
    {
        private volatile IDelegatingHandler _nextHandler;
        protected volatile bool _disposed;

        protected DefaultDelegatingHandler(PipelineContext context, IDelegatingHandler nextHandler)
        {
            Context = context;
            _nextHandler = nextHandler;

            if (Logging.IsEnabled)
                Logging.Associate(this, _nextHandler, nameof(NextHandler));
        }

        public PipelineContext Context { get; protected set; }

        public ContinuationFactory<IDelegatingHandler> ContinuationFactory { get; set; }

        public IDelegatingHandler NextHandler
        {
            get => _nextHandler;
            protected set
            {
                _nextHandler = value;

                if (Logging.IsEnabled)
                    Logging.Associate(this, _nextHandler, nameof(NextHandler));
            }
        }

        public virtual Task OpenAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.OpenAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task CloseAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (NextHandler == null)
            {
                return TaskHelpers.CompletedTask;
            }

            Task closeTask = NextHandler.CloseAsync(cancellationToken);
            return closeTask;
        }

        /// <summary>
        /// Completes when the transport disconnected.
        /// </summary>
        public virtual Task WaitForTransportClosedAsync()
        {
            ThrowIfDisposed();

            if (NextHandler == null)
            {
                throw new InvalidOperationException();
            }

            return NextHandler.WaitForTransportClosedAsync();
        }

        public virtual Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler.ReceiveMessageAsync(cancellationToken);
        }

        public virtual Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler.EnableReceiveMessageAsync(cancellationToken);
        }

        public virtual Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler.DisableReceiveMessageAsync(cancellationToken);
        }

        public virtual Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.CompleteMessageAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.AbandonMessageAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.RejectMessageAsync(lockToken, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendEventAsync(message, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendEventAsync(messages, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.EnableMethodsAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.DisableMethodsAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendMethodResponseAsync(methodResponse, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.EnableTwinPatchAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.DisableTwinPatchAsync(cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendTwinGetAsync(cancellationToken) ?? Task.FromResult((Twin)null);
        }

        public virtual Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendTwinPatchAsync(reportedProperties, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task EnableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.EnableEventReceiveAsync(isAnEdgeModule, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual Task DisableEventReceiveAsync(bool isAnEdgeModule, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.DisableEventReceiveAsync(isAnEdgeModule, cancellationToken) ?? TaskHelpers.CompletedTask;
        }

        public virtual bool IsUsable => NextHandler?.IsUsable ?? true;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("IoT Client");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(DefaultDelegatingHandler)}.{nameof(Dispose)}");

                if (!_disposed)
                {
                    if (disposing)
                    {
                        _nextHandler?.Dispose();
                    }

                    _disposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(DefaultDelegatingHandler)}.{nameof(Dispose)}");
            }
        }

        ~DefaultDelegatingHandler()
        {
            Dispose(false);
        }
    }
}
