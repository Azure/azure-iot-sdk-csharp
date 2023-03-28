﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    /// <summary>
    /// IoT hub publishes these event types.
    /// </summary>
    public enum DeviceEventOperationType
    {
        /// <summary>
        /// Published when a device is registered to an IoT hub.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Published when a device is connected to an IoT hub.
        /// </summary>
        [EnumMember(Value = "deviceConnected")]
        DeviceConnected,

        /// <summary>
        /// Published when a device is disconnected from an IoT hub.
        /// </summary>
        [EnumMember(Value = "deviceDisconnected")]
        DeviceDisconnected,

        /// <summary>
        /// A twin has changed.
        /// </summary>
        [EnumMember(Value = "updateTwin")]
        UpdateTwin,
    }
}
