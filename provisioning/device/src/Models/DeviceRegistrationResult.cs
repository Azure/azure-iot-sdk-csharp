// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The result of a registration operation.
    /// </summary>
    public class DeviceRegistrationResult
    {
        /// <summary>
        /// For deserialization and unit testing.
        /// </summary>
        protected internal DeviceRegistrationResult()
        {

        }

        /// <summary>
        /// This id is used to uniquely identify a device registration of an enrollment.
        /// </summary>
        [JsonPropertyName("registrationId")]
        public string RegistrationId { get; protected internal set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; protected internal set; }

        /// <summary>
        /// The assigned Azure IoT hub.
        /// </summary>
        [JsonPropertyName("assignedHub")]
        public string AssignedHub { get; protected internal set; }

        /// <summary>
        /// The Device Id.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        [JsonPropertyName("status")]
        public ProvisioningRegistrationStatusType Status { get; protected internal set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        [JsonPropertyName("substatus")]
        public ProvisioningRegistrationSubstatusType Substatus { get; protected internal set; }

        /// <summary>
        /// The generation Id.
        /// </summary>
        [JsonPropertyName("generationId")]
        public string GenerationId { get; protected internal set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        public DateTimeOffset? LastUpdatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonPropertyName("errorCode")]
        public int? ErrorCode { get; protected internal set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; protected internal set; }

        /// <summary>
        /// The entity tag associated with the resource.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; protected internal set; }

        /// <summary>
        /// The custom data returned from the webhook to the device.
        /// </summary>
        [JsonPropertyName("payload")]
        public JRaw Payload { get; protected internal set; }

        /// <summary>
        /// The registration result for X.509 certificate authentication.
        /// </summary>
        [JsonPropertyName("x509")]
        public X509RegistrationResult X509 { get; protected internal set; }

        /// <summary>
        /// The registration result for symmetric key authentication.
        /// </summary>
        [JsonPropertyName("symmetricKey")]
        public SymmetricKeyRegistrationResult SymmetricKey { get; protected internal set; }
    }
}
