// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpUnitCreator
    {
        AmqpUnit CreateAmqpUnit(DeviceIdentity deviceIdentity, Func<MethodRequestInternal, Task> methodHandler, Action<AmqpMessage> twinMessageListener, Func<string, Message, Task> eventListener);
    }
}
