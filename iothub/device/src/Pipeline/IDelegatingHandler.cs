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

        // Telemetry.
        Task SendTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken);

        Task SendTelemetryAsync(IEnumerable<TelemetryMessage> messages, CancellationToken cancellationToken);

        Task EnableReceiveMessageAsync(CancellationToken cancellationToken);

        Task DisableReceiveMessageAsync(CancellationToken cancellationToken);

        // Methods.
        Task EnableMethodsAsync(CancellationToken cancellationToken);

        Task DisableMethodsAsync(CancellationToken cancellationToken);

        Task SendMethodResponseAsync(DirectMethodResponse methodResponse, CancellationToken cancellationToken);

        // Twin.
        Task<TwinProperties> GetTwinAsync(CancellationToken cancellationToken);

        Task<long> UpdateReportedPropertiesAsync(ReportedProperties reportedProperties, CancellationToken cancellationToken);

        Task EnableTwinPatchAsync(CancellationToken cancellationToken);

        Task DisableTwinPatchAsync(CancellationToken cancellationToken);

        // HTTP specific operations
        Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken);

        Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken);

        // This is for invoking methods from an edge module to another edge device or edge module.
        Task<DirectMethodResponse> InvokeMethodAsync(EdgeModuleDirectMethodRequest methodInvokeRequest, Uri uri, CancellationToken cancellationToken);

        // Sas token validity
        Task<DateTime> RefreshSasTokenAsync(CancellationToken cancellationToken);

        DateTime GetSasTokenRefreshesOn();

        void SetSasTokenRefreshesOn();

        Task StopSasTokenLoopAsync();
    }
}
