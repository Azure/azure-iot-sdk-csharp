// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpSessionCreator
    {
        Task<AmqpIoTSession> CreateSession(DeviceIdentity deviceIdentity, TimeSpan timeout);
        Task<AmqpIoTConnection> EnsureConnection(TimeSpan timeout);
    }
}
