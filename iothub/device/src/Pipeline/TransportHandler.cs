// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal abstract class TransportHandler : DefaultDelegatingHandler
    {
        private TaskCompletionSource<bool> _transportShouldRetry;
        protected TransportSettings _transportSettings;

        protected TransportHandler(PipelineContext context, TransportSettings transportSettings)
            : base(context, nextHandler: null)
        {
            _transportSettings = transportSettings;
        }

        public override Task WaitForTransportClosedAsync()
        {
            _transportShouldRetry = new TaskCompletionSource<bool>();
            return _transportShouldRetry.Task;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(TransportHandler)}.{nameof(Dispose)}");

                if (!_disposed)
                {
                    base.Dispose(disposing);
                    if (disposing)
                    {
                        OnTransportClosedGracefully();
                    }

                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(TransportHandler)}.{nameof(Dispose)}");
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
