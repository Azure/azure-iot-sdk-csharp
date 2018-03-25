// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if ENABLE_MODULES_SDK
namespace Microsoft.Azure.Devices.Shared
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConfigurationStatus
    {
        Targeted = 1,
        Applied = 2,
    }
}
#endif