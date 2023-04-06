// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client.Pipeline
{
    internal sealed class ConnectionStatusHandler : DefaultDelegatingHandler
    {
        private readonly Action<ConnectionStatusInfo> _onConnectionStatusChanged;
        private long _clientTransportStatus; // references the current client transport status as the int value of ClientTransportStatus

        internal ConnectionStatusHandler(PipelineContext context, IDelegatingHandler innerHandler)
            : base(context, innerHandler)
        {
            _onConnectionStatusChanged = context.ConnectionStatusChangeHandler;
        }

        public override async Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, message, cancellationToken, nameof(SendTelemetryAsync));

            try
            {
                if (GetClientTransportStatus() == ClientTransportStatus.Open)
                {
                    await base.SendTelemetryAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, message, cancellationToken, nameof(SendTelemetryAsync));
            }
        }

        private ClientTransportStatus GetClientTransportStatus()
        {
            return (ClientTransportStatus)Interlocked.Read(ref _clientTransportStatus);
        }

        private void SetClientTransportStatus(ClientTransportStatus clientTransportStatus)
        {
            _ = Interlocked.Exchange(ref _clientTransportStatus, (int)clientTransportStatus);
        }
    }
}
