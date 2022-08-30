// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum AccessRights
    {
        RegistryRead = 1,
        RegistryWrite = RegistryRead | 2,
        ServiceConnect =  4,
        DeviceConnect = 8,
    }
}
