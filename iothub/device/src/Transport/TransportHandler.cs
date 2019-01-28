// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    abstract class TransportHandler : DefaultDelegatingHandler
    {
        protected ITransportSettings TransportSettings;
        protected TaskCompletionSource<bool> _transportShouldRetry = new TaskCompletionSource<bool>();

        protected TransportHandler(IPipelineContext context, ITransportSettings transportSettings)
            : base(context, innerHandler: null)
        {
            this.TransportSettings = transportSettings;
        }

        public override Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return this.ReceiveAsync(this.TransportSettings.DefaultReceiveTimeout, cancellationToken);
        }

        public override Task WaitForTransportClosedAsync()
        {
            return _transportShouldRetry.Task;
        }

        protected override void Dispose(bool disposing)
        {
            _transportShouldRetry.TrySetCanceled();
        }
    }
}
