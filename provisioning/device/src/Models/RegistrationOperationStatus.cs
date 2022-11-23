// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Registration operation status.
    /// </summary>
    internal sealed class RegistrationOperationStatus
    {
        /// <summary>
        /// Gets or sets operation Id.
        /// </summary>
        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets device enrollment status. Possible values include:
        /// 'unassigned', 'assigning', 'assigned', 'failed', 'disabled'
        /// </summary>
        [JsonPropertyName("status")]
        public ProvisioningRegistrationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets device registration status.
        /// </summary>
        [JsonPropertyName("registrationState")]
        public DeviceRegistrationResult RegistrationState { get; set; }

        /// <summary>
        /// Gets or sets the Retry-After header.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? RetryAfter { get; set; }
    }
}
