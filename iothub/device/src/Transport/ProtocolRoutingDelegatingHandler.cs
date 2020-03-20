// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Transport handler router.
    /// Tries to open the connection in the protocol order it was set.
    /// If fails tries to open the next one, etc.
    /// </summary>
    internal class ProtocolRoutingDelegatingHandler : DefaultDelegatingHandler
    {
        /// <summary>
        /// After we've verified that we could open the transport for any operation, we will stop attempting others in the list.
        /// </summary>
        private bool _transportSelectionComplete = false;

        private int _nextTransportIndex = 0;
        private SemaphoreSlim _handlerLock = new SemaphoreSlim(1, 1);

        internal delegate IDelegatingHandler TransportHandlerFactory(IotHubConnectionString iotHubConnectionString, ITransportSettings transportSettings);

        public ProtocolRoutingDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler) :
            base(context, innerHandler)
        {
        }

        public override async Task OpenAsync(TimeoutHelper timeoutHelper)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, timeoutHelper, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");

                bool gain = await _handlerLock.WaitAsync(timeoutHelper.RemainingTime()).ConfigureAwait(false);
                if (!gain) throw new TimeoutException("Timed out acquiring handler lock.");
                SelectNextTransport();

                try
                {
                    CreateNextTransportHandlerIfReady();
                    await base.OpenAsync(timeoutHelper).ConfigureAwait(false);

                    if (_disposed)
                    {
                        InnerHandler?.Dispose();
                        ThrowIfDisposed();
                    }
                    _transportSelectionComplete = true;
                }
                finally
                {
                    _handlerLock.Release();
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, timeoutHelper, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");
            }
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");
                cancellationToken.ThrowIfCancellationRequested();
                await _handlerLock.WaitAsync().ConfigureAwait(false);
                SelectNextTransport();

                try
                {
                    CreateNextTransportHandlerIfReady();
                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);

                    if (_disposed)
                    {
                        InnerHandler?.Dispose();
                        ThrowIfDisposed();
                    }
                    _transportSelectionComplete = true;
                }
                finally
                {
                    _handlerLock.Release();
                }
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, cancellationToken, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");
            }
        }

        public override async Task WaitForTransportClosedAsync()
        {
            // Will throw OperationCancelledException if CloseAsync() or Dispose() has been called by the application.
            await base.WaitForTransportClosedAsync().ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Info(this, "Client disconnected.", nameof(WaitForTransportClosedAsync));

            await _handlerLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Debug.Assert(InnerHandler != null);

                CreateNextTransportHandlerIfReady();
            }
            finally
            {
                _handlerLock.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handlerLock?.Dispose();
                _handlerLock = null;
            }

            base.Dispose(disposing);
        }

        private void SelectNextTransport()
        {
            if (!_transportSelectionComplete)
            {
                // Try next protocol if we're still searching.

                ITransportSettings[] transportSettingsArray = Context.Get<ITransportSettings[]>();
                Debug.Assert(transportSettingsArray != null);

                // Keep cycling through all transports until we find one that works.
                if (_nextTransportIndex >= transportSettingsArray.Length) _nextTransportIndex = 0;

                ITransportSettings transportSettings = transportSettingsArray[_nextTransportIndex];
                Debug.Assert(transportSettings != null);

                if (Logging.IsEnabled) Logging.Info(
                    this,
                    $"Trying {transportSettings?.GetTransportType()}",
                    $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");

                // Configure the transportSettings for this context (Important! Within Context, 'ITransportSettings' != 'ITransportSettings[]').
                Context.Set(transportSettings);
                CreateNextTransportHandler();

                _nextTransportIndex++;
            }
        }

        private void CreateNextTransportHandlerIfReady()
        {
            if (InnerHandler == null || !InnerHandler.IsUsable)
            {
                CreateNextTransportHandler();
            }
        }

        private void CreateNextTransportHandler()
        {
            IDelegatingHandler formerHandler = InnerHandler;

            // Ask the ContinuationFactory to attach the proper handler given the Context's ITransportSettings.
            InnerHandler = ContinuationFactory(Context, null);

            formerHandler?.Dispose();
        }
    }
}
