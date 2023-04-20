// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
