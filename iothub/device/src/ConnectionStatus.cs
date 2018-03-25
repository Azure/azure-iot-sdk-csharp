// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Connection Status supported by DeviceClient
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected = 1,
        Connected = 1 << 1,
        Disconnected_Retrying = 1 << 2,
        Disabled = 1 << 3,
    }
}
