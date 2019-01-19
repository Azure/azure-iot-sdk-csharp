// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Transport handler router. 
    /// Tries to open the connection in the protocol order it was set. 
    /// If fails tries to open the next one, etc.
    /// </summary>
    internal class ProtocolRoutingDelegatingHandler : DefaultDelegatingHandler
    {
        internal delegate IDelegatingHandler TransportHandlerFactory(IotHubConnectionString iotHubConnectionString, ITransportSettings transportSettings);

        /// <summary>
        /// After we've verified that we could open the transport for any operation, we will stop attempting others in the list.
        /// </summary>
        private bool _transportSelectionComplete = false;
        private int _nextTransportIndex = 0;

        private SemaphoreSlim _handlerLock = new SemaphoreSlim(1, 1);

        public ProtocolRoutingDelegatingHandler(IPipelineContext context, IDelegatingHandler innerHandler) :
            base(context, innerHandler)
        {
        }

        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, cancellationToken, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");
                cancellationToken.ThrowIfCancellationRequested();

                await _handlerLock.WaitAsync().ConfigureAwait(false);

                if (!_transportSelectionComplete)
                {
                    // Try next protocol if we're still searching.

                    ITransportSettings[] transportSettingsArray = this.Context.Get<ITransportSettings[]>();
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
                    Context.Set<ITransportSettings>(transportSettings);
                    CreateNewTransportHandler();

                    _nextTransportIndex++;
                }

                try
                {
                    await base.OpenAsync(cancellationToken).ConfigureAwait(false);
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

        private void CreateNewTransportHandler()
        {
            if (InnerHandler != null)
            {
                InnerHandler.Dispose();
                InnerHandler = null;
            }

            // Ask the ContinuationFactory to attach the proper handler given the Context's ITransportSettings.
            InnerHandler = ContinuationFactory(Context, null);
        }

        public override async Task WaitForTransportClosedAsync()
        {
            await base.WaitForTransportClosedAsync().ConfigureAwait(false);

            if (Logging.IsEnabled) Logging.Info(this, "Client disconnected.", nameof(WaitForTransportClosedAsync));

            await _handlerLock.WaitAsync().ConfigureAwait(false);
            Debug.Assert(InnerHandler != null);
            CreateNewTransportHandler();

            // Operations above should never throw. If they do, it's not safe to continue.
            _handlerLock.Release();
        }
    }
}
