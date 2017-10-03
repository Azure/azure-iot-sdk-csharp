// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device registration operation status.
    /// </summary>
    public class RegistrationOperationStatus
    {
        /// <summary>
        /// Operation ID.
        /// </summary>
        [JsonProperty(PropertyName = "operationId")]
        public string OperationId { get; set; }

        /// <summary>
        /// Enrollment status.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public EnrollmentStatus Status { get; set; }

        /// <summary>
        /// Registration status.
        /// </summary>
        [JsonProperty(PropertyName = "registrationStatus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RegistrationStatus RegistrationStatus { get; set; }
    }
}
