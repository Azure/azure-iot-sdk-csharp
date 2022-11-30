// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Registration status.
    /// </summary>
    public class DeviceRegistrationState
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DeviceRegistrationState()
        { }

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
        [JsonInclude]
        public string RegistrationId { get; protected internal set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        [JsonInclude]
        public DateTimeOffset? CreatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Last updated date time (in UTC).
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        [JsonInclude]
        public DateTimeOffset? LastUpdatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Assigned IoT hub.
        /// </summary>
        [JsonPropertyName("assignedHub")]
        [JsonInclude]
        public string AssignedHub { get; protected internal set; }

        /// <summary>
        /// Device Id.
        /// </summary>
        [JsonPropertyName("deviceId")]
        [JsonInclude]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// Status.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonInclude]
        public EnrollmentStatus Status { get; protected internal set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonPropertyName("errorCode")]
        [JsonInclude]
        public int? ErrorCode { get; protected internal set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        [JsonInclude]
        public string ErrorMessage { get; protected internal set; }

        /// <summary>
        /// Registration status ETag.
        /// </summary>
        [JsonPropertyName("etag")]
        [JsonInclude]
        public ETag ETag { get; protected internal set; }
    }
}
