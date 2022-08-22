// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Input specific to a job used in JobRequest.
    /// </summary>
    public class JobParameters
    {
        /// <summary>
        /// Construct the parameters for a device job.
        /// </summary>
        /// <param name="jobType">The type of job to run on the device</param>
        public JobParameters(JobType jobType)
        {
            JobType = jobType;
        }

        /// <summary>
        /// The type of job to execute.
        /// </summary>
        [JsonProperty(PropertyName = "jobType", Required = Required.Always)]
        public JobType JobType { get; private set; }
    }
}
