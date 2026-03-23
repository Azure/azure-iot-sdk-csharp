// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("registrationId")]
        public string RegistrationId { get; set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; set; }

        /// <summary>
        /// Last updated date time (in UTC).
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        public DateTimeOffset? LastUpdatedOnUtc { get; set; }

        /// <summary>
        /// Assigned IoT hub.
        /// </summary>
        [JsonPropertyName("assignedHub")]
        public string AssignedHub { get; set; }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Status.
        /// </summary>
        [JsonPropertyName("status")]
        public EnrollmentStatus Status { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int? ErrorCode { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Registration status ETag.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }
    }
}
