// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Extend job parameters with device Ids.
    /// </summary>
    public class DeviceJobParameters : JobParameters
    {
        /// <summary>
        /// Parameters for parameterless device job on a single device.
        /// </summary>
        public DeviceJobParameters(JobType jobType, string deviceId)
            : this(jobType, new List<string>() { deviceId })
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
        }

        /// <summary>
        /// Parameters for parameterless device job on multiple devices.
        /// </summary>
        public DeviceJobParameters(JobType jobType, IEnumerable<string> deviceIds)
            : base(jobType)
        {
            Argument.AssertNotNull(deviceIds, nameof(deviceIds));
            DeviceIds = deviceIds;
        }

        /// <summary>
        /// Ids of target devices.
        /// </summary>
        public IEnumerable<string> DeviceIds { get; private set; }
    }
}
