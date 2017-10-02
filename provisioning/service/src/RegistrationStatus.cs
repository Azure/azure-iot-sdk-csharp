// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Registration status.
    /// </summary>
    public class RegistrationStatus : IETagHolder
    {
        /// <summary>
        /// Creates a new instance of <see cref="RegistrationStatus"/>
        /// </summary>
        /// <param name="registrationId">Registration ID</param>
        public RegistrationStatus(string registrationId)
        {
            this.RegistrationId = registrationId;
        }

        /// <summary>
        /// Registration ID.
        /// </summary>
        [JsonProperty(PropertyName = "registrationId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RegistrationId { get; set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? CreatedDateTimeUtc { get; set; }

        /// <summary>
        /// Last updated date time (in UTC).
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? LastUpdatedDateTimeUtc { get; set; }

        /// <summary>
        /// Assigned IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "assignedHub", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssignedHub { get; set; }

        /// <summary>
        /// Device ID.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeviceId { get; set; }

        /// <summary>
        /// Status.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public EnrollmentStatus Status { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty(PropertyName = "errorCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ErrorCode { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Registration status ETag
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ETag { get; set; }
    }

}
