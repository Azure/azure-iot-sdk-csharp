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
        internal string OperationId { get; set; }

        /// <summary>
        /// Gets or sets device enrollment status.
        /// </summary>
        [JsonPropertyName("status")]
        internal ProvisioningRegistrationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets device registration status.
        /// </summary>
        [JsonPropertyName("registrationState")]
        internal DeviceRegistrationResult RegistrationState { get; set; }

        /// <summary>
        /// Gets or sets the Retry-After header.
        /// </summary>
        internal TimeSpan? RetryAfter { get; set; }
    }
}
