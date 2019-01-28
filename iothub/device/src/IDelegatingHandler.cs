// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Shared;

    interface IDelegatingHandler : IContinuationProvider<IDelegatingHandler>, IDisposable
    {
        // Transport state.
        Task OpenAsync(CancellationToken cancellationToken);
        Task CloseAsync(CancellationToken cancellationToken);
        Task WaitForTransportClosedAsync();

        // Telemetry uplink.
        Task SendEventAsync(Message message, CancellationToken cancellationToken);
        Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);

        // Telemetry downlink.
        Task<Message> ReceiveAsync(CancellationToken cancellationToken);
        Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken);
        Task RejectAsync(string lockToken, CancellationToken cancellationToken);
        Task AbandonAsync(string lockToken, CancellationToken cancellationToken);
        Task CompleteAsync(string lockToken, CancellationToken cancellationToken);

        // Telemetry downlink for modules.
        Task EnableEventReceiveAsync(CancellationToken cancellationToken);
        Task DisableEventReceiveAsync(CancellationToken cancellationToken);

        // Methods.
        Task EnableMethodsAsync(CancellationToken cancellationToken);
        Task DisableMethodsAsync(CancellationToken cancellationToken);
        Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken);

        // Twin.
        Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken);
        Task SendTwinPatchAsync(TwinCollection reportedProperties, CancellationToken cancellationToken);
        Task EnableTwinPatchAsync(CancellationToken cancellationToken);
    }
}
