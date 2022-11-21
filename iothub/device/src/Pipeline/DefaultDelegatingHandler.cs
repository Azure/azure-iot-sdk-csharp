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
        protected volatile bool _isDisposed;

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
            return NextHandler?.OpenAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task CloseAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (NextHandler == null)
            {
                return Task.CompletedTask;
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

            return NextHandler == null
                ? throw new InvalidOperationException()
                : NextHandler.WaitForTransportClosedAsync();
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

        public virtual Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendTelemetryAsync(message, cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task SendTelemetryBatchAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendTelemetryBatchAsync(messages, cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task EnableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.EnableMethodsAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task DisableMethodsAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.DisableMethodsAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.SendMethodResponseAsync(methodResponse, cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task EnableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.EnableTwinPatchAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task DisableTwinPatchAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.DisableTwinPatchAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public virtual Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.GetTwinAsync(cancellationToken) ?? Task.FromResult((TwinProperties)null);
        }

        public virtual Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return NextHandler?.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken) ?? Task.FromResult(0L);
        }

        public virtual bool IsUsable => NextHandler?.IsUsable ?? true;

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("IoT Client");
            }
        }

        protected private virtual void Dispose(bool disposing)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Disposed={_isDisposed}; disposing={disposing}", $"{nameof(DefaultDelegatingHandler)}.{nameof(Dispose)}");

            try
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        _nextHandler?.Dispose();
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Disposed={_isDisposed}; disposing={disposing}", $"{nameof(DefaultDelegatingHandler)}.{nameof(Dispose)}");
            }
        }

        ~DefaultDelegatingHandler()
        {
            Dispose(false);
        }
    }
}
