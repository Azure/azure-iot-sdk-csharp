﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal interface IAmqpUnitManager
    {
        AmqpUnit CreateAmqpUnit(
            DeviceIdentity deviceIdentity,
            Func<MethodRequestInternal, Task> onMethodCallback,
            Action<Stream, string, TwinCollection, IotHubException> twinMessageListener,
            Func<string, Message, Task> onModuleMessageReceivedCallback,
            Func<Message, Task> onDeviceMessageReceivedCallback,
            Action onUnitDisconnected);

        void RemoveAmqpUnit(AmqpUnit amqpUnit);
    }
}
