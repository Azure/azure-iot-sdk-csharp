// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of twin scheduled job.
    /// </summary>
    public class TwinScheduledJob : ScheduledJob
    {
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal TwinScheduledJob()
        { }

        /// <summary>
        /// Creates an instance of this class for twin scheduled job.
        /// </summary>
        /// <param name="updateTwin">The update twin tags and desired properties.</param>
        protected internal TwinScheduledJob(Twin updateTwin)
        {
            JobType = JobType.ScheduleUpdateTwin;
            UpdateTwin = updateTwin;
        }

        /// <summary>
        /// The update twin tags and desired properties.
        /// </summary>
        [JsonProperty(PropertyName = "updateTwin", NullValueHandling = NullValueHandling.Ignore)]
        public Twin UpdateTwin { get; }
    }
}
