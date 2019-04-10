// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpConnectionHolder : IStatusReportor, IDisposable
    {
        Task<IAmqpAuthenticationRefresher> StartAmqpAuthenticationRefresherAsync(DeviceIdentity deviceIdentity, TimeSpan timeout);
        Task<AmqpSession> CreateAmqpSessionAsync(DeviceIdentity deviceIdentity, TimeSpan timeout);
        void Close();
    }
}
