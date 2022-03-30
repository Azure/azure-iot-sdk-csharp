// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Registration operation status.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    internal partial class RegistrationOperationStatus
    {
        public const string OperationStatusAssigned = "assigned";
        public const string OperationStatusAssigning = "assigning";
        public const string OperationStatusUnassigned = "unassigned";

        /// <summary>
        /// Initializes a new instance of the RegistrationOperationStatus
        /// class.
        /// </summary>
        /// <param name="operationId">Operation Id.</param>
        /// <param name="status">Device enrollment status. Possible values
        /// include: 'unassigned', 'assigning', 'assigned', 'failed',
        /// 'disabled'</param>
        /// <param name="registrationState">Device registration
        /// status.</param>
        public RegistrationOperationStatus(
            string operationId = default,
            string status = default,
            DeviceRegistrationResult registrationState = default)
        {
            OperationId = operationId;
            Status = status;
            RegistrationState = registrationState;
        }

        /// <summary>
        /// Gets or sets operation Id.
        /// </summary>
        [JsonProperty(PropertyName = "operationId")]
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets device enrollment status. Possible values include:
        /// 'unassigned', 'assigning', 'assigned', 'failed', 'disabled'
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets device registration status.
        /// </summary>
        [JsonProperty(PropertyName = "registrationState")]
        public DeviceRegistrationResult RegistrationState { get; set; }

        /// <summary>
        /// Gets or sets the Retry-After header.
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }
    }
}
