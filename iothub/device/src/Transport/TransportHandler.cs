// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal abstract class TransportHandler : DefaultDelegatingHandler
    {
        private TaskCompletionSource<bool> _transportShouldRetry;
        protected ITransportSettings _transportSettings;

        protected TransportHandler(PipelineContext context, ITransportSettings transportSettings)
            : base(context, innerHandler: null)
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
            if (_disposed)
            {
                return;
            }

            base.Dispose(disposing);
            if (disposing)
            {
                OnTransportClosedGracefully();
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
