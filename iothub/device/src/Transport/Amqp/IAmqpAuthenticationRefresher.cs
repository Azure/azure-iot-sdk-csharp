// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpAuthenticationRefresher : IDisposable
    {
        Task InitLoopAsync(TimeSpan timeout);
        void StartLoop(DateTime refreshOn, CancellationToken cancellationToken);
        Task StopLoopAsync();
    }
}
