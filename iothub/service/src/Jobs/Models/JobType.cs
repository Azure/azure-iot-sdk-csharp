// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to specify the type of job.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Indicates an export devices job.
        /// </summary>
        Export,

        /// <summary>
        /// Indicates an import devices job.
        /// </summary>
        Import,

        /// <summary>
        /// Indicates a scheduled device method job.
        /// </summary>
        ScheduleDeviceMethod,

        /// <summary>
        /// Indicates a scheduled twin update job.
        /// </summary>
        ScheduleUpdateTwin,
    }
}
