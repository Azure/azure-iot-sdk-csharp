// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Transport delegating handler.
    /// This handler is responsible for initializing the transport handler on initial connection and subsequent reconnects.
    /// </summary>
    internal sealed class TransportDelegatingHandler : DefaultDelegatingHandler
    {
        private readonly SemaphoreSlim _handlerLock = new(1, 1);

        public TransportDelegatingHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, cancellationToken, $"{nameof(TransportDelegatingHandler)}.{nameof(OpenAsync)}");

            cancellationToken.ThrowIfCancellationRequested();

            await _handlerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CreateNewTransportIfNotReady();
                await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                // since Dispose is not synced with _handlerLock, double check if disposed.
                if (_isDisposed)
                {
                    NextHandler?.Dispose();
                    ThrowIfDisposed();
                }
            }
            finally
            {
                _handlerLock.Release();
                if (Logging.IsEnabled)
                    Logging.Exit(this, cancellationToken, $"{nameof(TransportDelegatingHandler)}.{nameof(OpenAsync)}");
            }
        }

        public override async Task WaitForTransportClosedAsync()
        {
            // Will throw OperationCancelledException if CloseAsync() or Dispose() has been called by the application.
            await base.WaitForTransportClosedAsync().ConfigureAwait(false);

            if (Logging.IsEnabled)
                Logging.Info(this, "Client disconnected.", nameof(WaitForTransportClosedAsync));

            await _handlerLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Debug.Assert(NextHandler != null);

                // We don't need to double check since it's being handled in OpenAsync
                CreateNewTransportIfNotReady();
            }
            finally
            {
                // Operations above should never throw. If they do, it's not safe to continue.
                _handlerLock.Release();
            }
        }

        protected private override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _handlerLock?.Dispose();
        }

        private void CreateNewTransportIfNotReady()
        {
            if (NextHandler == null || !NextHandler.IsUsable)
            {
                IDelegatingHandler innerHandler = NextHandler;

                // Ask the ContinuationFactory to attach the proper handler given the Context's ITransportSettings.
                NextHandler = ContinuationFactory(Context, null);

                innerHandler?.Dispose();
            }
        }
    }
}
