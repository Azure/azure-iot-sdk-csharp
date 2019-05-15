// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal interface IAmqpIoTUnitManager
    {
        AmqpIoTUnit CreateAmqpUnit(DeviceIdentity deviceIdentity, Func<MethodRequestInternal, Task> methodHandler, Action<AmqpIoTMessage> twinMessageListener, Func<string, Message, Task> eventListener);
    }
}
