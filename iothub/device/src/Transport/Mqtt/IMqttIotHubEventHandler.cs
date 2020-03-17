﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal interface IMqttIotHubEventHandler
    {
        void OnConnected();

        void OnMessageReceived(Message message);

        void OnError(Exception exception);

        TransportState State { get; }
    }
}
