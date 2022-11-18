// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    internal enum AccessRights
    {
        RegistryRead = 1,
        RegistryWrite = RegistryRead | 2,
        ServiceConnect =  4,
        DeviceConnect = 8,
    }
}
