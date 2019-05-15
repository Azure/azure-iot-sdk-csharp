// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTConnector
    {
        void Dispose();
        Task<AmqpConnection> OpenConnectionAsync(TimeSpan timeout);
    }
}