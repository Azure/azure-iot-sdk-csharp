// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Specifies the options associated with job queries.
    /// </summary>
    public class JobQueryOptions
    {
        /// <summary>
        /// The job type to query.
        /// </summary>
        /// <remarks>
        /// If null, jobs of all types will be returned.
        /// </remarks>
        public JobType? JobType { get; set; }

        /// <summary>
        /// The job status to query.
        /// </summary>
        /// <remarks>
        /// If null, jobs of all states will be returned.
        /// </remarks>
        public JobStatus? JobStatus {get; set; }
    }
}
