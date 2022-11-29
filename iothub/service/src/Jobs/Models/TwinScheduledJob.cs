// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of twin scheduled job.
    /// </summary>
    public class TwinScheduledJob : ScheduledJob
    {
        /// <summary>
        /// For deserialization and unit testing.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TwinScheduledJob()
        { }

        /// <summary>
        /// Creates an instance of this class for twin scheduled job.
        /// </summary>
        /// <param name="updateTwin">The update twin tags and desired properties.</param>
        protected internal TwinScheduledJob(ClientTwin updateTwin)
        {
            JobType = JobType.ScheduleUpdateTwin;
            UpdateTwin = updateTwin;
        }

        /// <summary>
        /// The update twin tags and desired properties.
        /// </summary>
        [JsonPropertyName("updateTwin")]
        public ClientTwin UpdateTwin { get; }
    }
}
