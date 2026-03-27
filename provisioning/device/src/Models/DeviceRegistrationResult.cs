// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The result of a registration operation.
    /// </summary>
    public class DeviceRegistrationResult
    {
        /// <summary>
        /// This id is used to uniquely identify a device registration of an enrollment.
        /// </summary>
        [JsonPropertyName("registrationId")]
        public string RegistrationId { get; set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; set; }

        /// <summary>
        /// The assigned Azure IoT hub.
        /// </summary>
        [JsonPropertyName("assignedHub")]
        public string AssignedHub { get; set; }

        /// <summary>
        /// The Device Id.
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        [JsonPropertyName("status")]
        public ProvisioningRegistrationStatus Status { get; set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        [JsonPropertyName("substatus")]
        public ProvisioningRegistrationSubstatus Substatus { get; set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        public DateTimeOffset? LastUpdatedOnUtc { get; set; }

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
        /// The entity tag associated with the resource.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }

        /// <summary>
        /// The custom data returned from the webhook to the device.
        /// </summary>
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }

        /// <summary>
        /// The registration result for X.509 certificate authentication.
        /// </summary>
        [JsonPropertyName("x509")]
        public X509RegistrationResult X509 { get; set; }

        /// <summary>
        /// The registration result for symmetric key authentication.
        /// </summary>
        [JsonPropertyName("symmetricKey")]
        public SymmetricKeyRegistrationResult SymmetricKey { get; set; }

        /// <summary>
        ///  Custom allocation payload (as a type) returned from the webhook to the device.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="value">The value of the payload.</param>
        /// <returns>True if the value can be converted to the specified type, otherwise false.</returns>
        public bool TryGetPayload<T>(out T value)
        {
            value = default;
            if (Payload == null 
                || Payload.Value.ValueKind == JsonValueKind.Null
                || Payload.Value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(Payload.Value, JsonSerializerSettings.Options);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
