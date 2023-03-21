// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
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
        [EnumMember(Value = "unknown")]
        Unknown,

        /// <summary>
        /// Indicates an export devices job.
        /// </summary>
        [EnumMember(Value = "export")]
        Export,

        /// <summary>
        /// Indicates an import devices job.
        /// </summary>
        [EnumMember(Value = "import")]
        Import,

        /// <summary>
        /// Indicates a scheduled device method job.
        /// </summary>
        [EnumMember(Value = "scheduleDeviceMethod")]
        ScheduleDeviceMethod,

        /// <summary>
        /// Indicates a scheduled twin update job.
        /// </summary>
        [EnumMember(Value = "scheduleUpdateTwin")]
        ScheduleUpdateTwin,
    }
}
