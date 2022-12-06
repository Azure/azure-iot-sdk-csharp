// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different connection states of a device or module.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClientConnectionState
    {
        /// <summary>
        /// Represents a device in the disconnected state.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Represents a device in the connected state.
        /// </summary>
        Connected,
    }
}
