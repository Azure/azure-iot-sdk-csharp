// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Registration operation status.
    /// </summary>
    public sealed class RegistrationOperationStatus
    {
        /// <summary>
        /// Gets or sets operation Id.
        /// </summary>
        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets device enrollment status.
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
        [JsonPropertyName("retryAfter")]
        public TimeSpan? RetryAfter { get; set; }
    }
}
