// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    using System;

    [Flags]
    public enum TransportState
    {
        NotInitialized = 1,
        Opening = 2,
        Open = 4,
        Subscribing = Open | 8,
        Receiving = Open | 16,
        Closed = 32,
        Error = 64
    }
}