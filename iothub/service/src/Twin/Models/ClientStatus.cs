﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different states of a device.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClientStatus
    {
        /// <summary>
        /// Indicates that a device is enabled.
        /// </summary>
        Enabled,

        /// <summary>
        /// Indicates that a device is disabled.
        /// </summary>
        Disabled,
    }
}
