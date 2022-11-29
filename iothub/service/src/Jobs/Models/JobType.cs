// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to specify the type of job.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum JobType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Indicates an ExportDevices job.
        /// </summary>
        ExportDevices,

        /// <summary>
        /// Indicates an ImportDevices job.
        /// </summary>
        ImportDevices,

        /// <summary>
        /// Indicates a Device method job.
        /// </summary>
        ScheduleDeviceMethod,

        /// <summary>
        /// Indicates a Twin update job.
        /// </summary>
        ScheduleUpdateTwin,
    }
}
