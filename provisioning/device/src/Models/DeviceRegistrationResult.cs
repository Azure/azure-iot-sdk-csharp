// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;

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
        { }

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
        public ProvisioningRegistrationStatus Status { get; protected internal set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        [JsonPropertyName("substatus")]
        public ProvisioningRegistrationSubstatus Substatus { get; protected internal set; }

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
        ///  Custom allocation payload (as a string) returned from the webhook to the device.
        /// </summary>
        [JsonIgnore]
        public string PayloadAsString => JsonPayload?.GetRawText();

        /// <summary>
        ///  Custom allocation payload returned from the webhook to the device.
        /// </summary>
        [JsonPropertyName("payload")]
        protected internal JsonElement? JsonPayload { get; set; }

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

        /// <summary>
        ///  Custom allocation payload (as a type) returned from the webhook to the device.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="value">The value of the payload.</param>
        /// <returns>True if the value can be converted to the specified type, otherwise false.</returns>
        public bool TryGetPayload<T>(out T value)
        {
            value = default;
            if (JsonPayload == null)
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(JsonPayload.Value.GetRawText());
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
