// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionHolder : IDisposable
    {
        Task<AmqpIotSession> OpenSessionAsync(DeviceIdentity deviceIdentity, CancellationToken cancellationToken);

        Task<AmqpIotConnection> EnsureConnectionAsync(CancellationToken cancellationToken);

        Task<IAmqpAuthenticationRefresher> CreateRefresherAsync(DeviceIdentity deviceIdentity, CancellationToken cancellationToken);

        void Shutdown();
    }
}
