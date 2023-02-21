// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        [JsonProperty("registrationId")]
        public string RegistrationId { get; protected internal set; }

        /// <summary>
        /// Registration create date time (in UTC).
        /// </summary>
        [JsonProperty("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; protected internal set; }

        /// <summary>
        /// The assigned Azure IoT hub.
        /// </summary>
        [JsonProperty("assignedHub")]
        public string AssignedHub { get; protected internal set; }

        /// <summary>
        /// The Device Id.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; protected internal set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        [JsonProperty("status")]
        public ProvisioningRegistrationStatus Status { get; protected internal set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        [JsonProperty("substatus")]
        public ProvisioningRegistrationSubstatus Substatus { get; protected internal set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        [JsonProperty("lastUpdatedDateTimeUtc")]
        public DateTimeOffset? LastUpdatedOnUtc { get; protected internal set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty("errorCode")]
        public int? ErrorCode { get; protected internal set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; protected internal set; }

        /// <summary>
        /// The entity tag associated with the resource.
        /// </summary>
        [JsonProperty("etag")]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly.
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; protected internal set; }

        /// <summary>
        /// The custom data returned from the webhook to the device.
        /// </summary>
        [JsonProperty("payload")]
        protected internal JRaw Payload { get; set; }

        /// <summary>
        /// The registration result for X.509 certificate authentication.
        /// </summary>
        [JsonProperty("x509")]
        public X509RegistrationResult X509 { get; protected internal set; }

        /// <summary>
        /// The registration result for symmetric key authentication.
        /// </summary>
        [JsonProperty("symmetricKey")]
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
            if (Payload == null)
            {
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(Payload.Value<string>());
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
