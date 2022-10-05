﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    internal interface IDelegatingHandler : IContinuationProvider<IDelegatingHandler>, IDisposable
    {
        // Transport state.
        Task OpenAsync(CancellationToken cancellationToken);

        Task CloseAsync(CancellationToken cancellationToken);

        Task WaitForTransportClosedAsync();

        bool IsUsable { get; }

        // Telemetry uplink.
        Task SendEventAsync(OutgoingMessage message, CancellationToken cancellationToken);

        Task SendEventAsync(IEnumerable<OutgoingMessage> messages, CancellationToken cancellationToken);

        Task EnableReceiveMessageAsync(CancellationToken cancellationToken);

        Task DisableReceiveMessageAsync(CancellationToken cancellationToken);

        // Methods.
        Task EnableMethodsAsync(CancellationToken cancellationToken);

        Task DisableMethodsAsync(CancellationToken cancellationToken);

        Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken);

        // Twin.
        Task<ClientTwin> GetTwinAsync(CancellationToken cancellationToken);

        Task<long> UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken);

        Task EnableTwinPatchAsync(CancellationToken cancellationToken);

        Task DisableTwinPatchAsync(CancellationToken cancellationToken);
    }
}
