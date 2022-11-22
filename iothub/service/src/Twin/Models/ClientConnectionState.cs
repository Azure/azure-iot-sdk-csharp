﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the different connection states of a device or module.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClientConnectionState
    {
        /// <summary>
        /// Represents a device in the disconnected state.
        /// </summary>
        [EnumMember(Value = "disconnected")]
        Disconnected = 0,

        /// <summary>
        /// Represents a device in the connected state.
        /// </summary>
        [EnumMember(Value = "connected")]
        Connected = 1,
    }
}