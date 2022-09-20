// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the configuration status.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConfigurationStatus
    {
        /// <summary>
        /// Configuration targeted.
        /// </summary>
        Targeted = 1,

        /// <summary>
        /// Configuration applied.
        /// </summary>
        Applied = 2,
    }
}
