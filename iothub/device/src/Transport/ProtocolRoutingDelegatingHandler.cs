// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Devices.Shared;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Transport handler router. 
    /// Tries to open the connection in the protocol order it was set. 
    /// If fails tries to open the next one, etc.
    /// </summary>
    class ProtocolRoutingDelegatingHandler : DefaultDelegatingHandler
    {
        internal delegate IDelegatingHandler TransportHandlerFactory(IotHubConnectionString iotHubConnectionString, ITransportSettings transportSettings);

        public ProtocolRoutingDelegatingHandler(IPipelineContext context):
            base(context)
        {

        }

        public override async Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, explicitOpen, cancellationToken, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");

                await this.TryOpenPrioritizedTransportsAsync(explicitOpen, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, explicitOpen, cancellationToken, $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(OpenAsync)}");
            }
        }

        async Task TryOpenPrioritizedTransportsAsync(bool explicitOpen, CancellationToken cancellationToken)
        {
            Exception lastException = null;

            // Concrete Device Client creation was deferred. Use prioritized list of transports.
            foreach (ITransportSettings transportSetting in this.Context.Get<ITransportSettings[]>())
            {

                if (Logging.IsEnabled) Logging.Info(
                    this,
                    $"Trying {transportSetting?.GetTransportType()}",
                    $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");

                if (cancellationToken.IsCancellationRequested)
                {
                    if (Logging.IsEnabled) Logging.Info(this, $"Cancellation requested for {Logging.GetHashCode(cancellationToken)}.", $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");
                    var tcs = new TaskCompletionSource<bool>();
                    tcs.SetCanceled();
                    await tcs.Task.ConfigureAwait(false);
                }

                try
                {
                    this.Context.Set(transportSetting);
                    this.InnerHandler = this.ContinuationFactory(this.Context);

                    // Try to open a connection with this transport
                    await base.OpenAsync(explicitOpen, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    try
                    {
                        if (this.InnerHandler != null)
                        {
                            await this.CloseAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        //ignore close failures
                        if (Logging.IsEnabled) Logging.Info(
                            this,
                            $"Exception caught while closing {transportSetting?.GetTransportType()}: {exception}",
                            $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");
                    }

                    if (!(exception is IotHubCommunicationException ||
                          exception is TimeoutException ||
                          exception is SocketException ||
                          exception is AggregateException))
                    {
                        if (Logging.IsEnabled) Logging.Error(this, $"Re-throwing exception caught: {exception}", $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");
                        throw;
                    }

                    var aggregateException = exception as AggregateException;
                    if (aggregateException != null)
                    {
                        ReadOnlyCollection<Exception> innerExceptions = aggregateException.Flatten().InnerExceptions;
                        if (!innerExceptions.Any(x => x is IotHubCommunicationException ||
                            x is SocketException ||
                            x is TimeoutException))
                        {
                            if (Logging.IsEnabled) Logging.Error(this, $"Re-throwing AggregateException: {exception}", $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");

                            throw;
                        }
                    }

                    lastException = exception;

                    if (Logging.IsEnabled) Logging.Error(this, $"Exception caught: {exception}", $"{nameof(ProtocolRoutingDelegatingHandler)}.{nameof(TryOpenPrioritizedTransportsAsync)}");

                    // open connection failed. Move to next transport type
                    continue;
                }

                return;
            }

            if (lastException != null)
            {
                throw new IotHubCommunicationException("Unable to open transport", lastException);
            }
        }
    }
}
