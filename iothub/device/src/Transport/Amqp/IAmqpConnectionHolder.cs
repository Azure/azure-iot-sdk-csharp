// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionHolder : IDisposable
    {
        Task<AmqpIotSession> OpenSessionAsync(IIotHubConnectionInfo iotHubConnectionInfo, CancellationToken cancellationToken);

        Task<AmqpIotConnection> EnsureConnectionAsync(CancellationToken cancellationToken);

        Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(IIotHubConnectionInfo iotHubConnectionInfo, CancellationToken cancellationToken);

        void Shutdown();
    }
}
