// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Base class for transport-specific handlers, i.e. MQTT and AMQP.
    /// </summary>
    internal abstract class TransportHandlerBase : DefaultDelegatingHandler
    {
        private TaskCompletionSource<bool> _transportShouldRetry;

        protected TransportHandlerBase(PipelineContext context, IDelegatingHandler nextHandler)
            : base(context, nextHandler)
        {
        }

        /// <summary>
        /// Twin operations are initiated one one link/topic and the response is received on another, where the
        /// waiting client call is completed via a TaskCompletionSource linked to the request Id.
        /// For various reasons, the device may never observe the response from the service and the client call will
        /// sit indefinitely, however unlikely. In order to prevent an ever increasing dictionary, we'll occasionally
        /// review these pending operations, and cancel/remove them from the dictionary.
        /// </summary>
        protected private static TimeSpan TwinResponseTimeout { get; } = TimeSpan.FromHours(1);

        public override Task WaitForTransportClosedAsync()
        {
            _transportShouldRetry = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _transportShouldRetry.Task;
        }

        protected private override void Dispose(bool disposing)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(TransportHandlerBase)}.{nameof(Dispose)}");

            try
            {
                if (!_isDisposed) // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                {
                    base.Dispose(disposing);
                    if (disposing)
                    {
                        OnTransportClosedGracefully();
                    }
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_isDisposed}; disposing={disposing}", $"{nameof(TransportHandlerBase)}.{nameof(Dispose)}");
            }
        }

        protected void OnTransportClosedGracefully()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, $"{nameof(OnTransportClosedGracefully)}");

            _transportShouldRetry?.TrySetCanceled();
        }

        protected void OnTransportDisconnected()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, $"{nameof(OnTransportDisconnected)}");

            _transportShouldRetry?.TrySetResult(true);
        }
    }
}
