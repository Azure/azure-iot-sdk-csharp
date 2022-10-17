// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Registration status.
    /// </summary>
    public class DeviceRegistrationState
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="registrationId">Registration Id</param>
        public DeviceRegistrationState(string registrationId)
        {
            RegistrationId = registrationId;
        }

        /// <summary>
        /// Registration Id.
        /// </summary>
        [JsonProperty(PropertyName = "registrationId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RegistrationId { get; internal set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? CreatedOnUtc { get; internal set; }

        /// <summary>
        /// Last updated date time (in UTC).
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? LastUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Assigned IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "assignedHub", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AssignedHub { get; internal set; }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Status.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public EnrollmentStatus Status { get; internal set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty(PropertyName = "errorCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ErrorCode { get; internal set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ErrorMessage { get; internal set; }

        /// <summary>
        /// Registration status ETag.
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))] // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly
        public ETag ETag { get; internal set; }
    }
}
