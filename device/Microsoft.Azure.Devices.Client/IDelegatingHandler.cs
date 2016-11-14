// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client.Common;

    // Temporary implementations until:
    // 1) Haitham gets his method class checked in.
    // 2) Bert refactors the Twin, etc, classes out of the shared SDK and into a common assembly
    class Method { }
    class Twin { }
    class TwinProperties { }

    delegate void TwinUpdateCallback(Twin twin, Boolean fullUdpate, TwinProperties state);
    delegate void MethodCallCallback(Method method);

    interface IDelegatingHandler: IDisposable
    {
        IDelegatingHandler InnerHandler { get; }

        Task AbandonAsync(string lockToken, CancellationToken cancellationToken);
        Task CloseAsync();
        Task CompleteAsync(string lockToken, CancellationToken cancellationToken);
        Task OpenAsync(bool explicitOpen, CancellationToken cancellationToken);
        Task<Message> ReceiveAsync(CancellationToken cancellationToken);
        Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken);
        Task RejectAsync(string lockToken, CancellationToken cancellationToken);
#if WINDOWS_UWP
        [Windows.Foundation.Metadata.DefaultOverload]
#endif
        Task SendEventAsync(Message message, CancellationToken cancellationToken);
        Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);

        Task EnableMethodsAsync(CancellationToken cancellationToken);
        Task SendMethodResponseAsync(Method method, CancellationToken ct);

        Task EnableTwinAsync(CancellationToken cancellationToken);
        Task SendTwinGetAsync(Twin twin, CancellationToken ct);
        Task SendTwinUpdateAsync(Twin twin, TwinProperties properties,  CancellationToken ct);


        TwinUpdateCallback TwinUpdateHandler { set; }

        MethodCallCallback MethodCallHandler { set; }


    }
}
