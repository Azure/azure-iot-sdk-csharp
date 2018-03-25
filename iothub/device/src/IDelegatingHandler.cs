// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Transport;
    using Microsoft.Azure.Devices.Shared;

    interface IDelegatingHandler : IContinuationProvider<IDelegatingHandler>, IDisposable
    {
        Task AbandonAsync(string lockToken, CancellationToken cancellationToken);
        Task CloseAsync();
        Task CompleteAsync(string lockToken, CancellationToken cancellationToken);
        Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken);
        Task<Message> ReceiveAsync(CancellationToken cancellationToken);
        Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken);
        Task RejectAsync(string lockToken, CancellationToken cancellationToken);
        Task SendEventAsync(Message message, CancellationToken cancellationToken);
        Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);

        Task EnableMethodsAsync(CancellationToken cancellationToken);
        Task DisableMethodsAsync(CancellationToken cancellationToken);
        Task SendMethodResponseAsync(MethodResponseInternal methodResponse, CancellationToken cancellationToken);
        Task RecoverConnections(object o, ConnectionType connectionType, CancellationToken cancellationToken);

        Task EnableTwinPatchAsync(CancellationToken cancellationToken);
        Task<Twin> SendTwinGetAsync(CancellationToken ct);
        Task SendTwinPatchAsync(TwinCollection reportedProperties,  CancellationToken ct);

        Task EnableEventReceiveAsync(CancellationToken cancellationToken);
        Task DisableEventReceiveAsync(CancellationToken cancellationToken);
    }
}
