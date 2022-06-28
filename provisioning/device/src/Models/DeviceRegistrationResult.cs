// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// For serialization.
        /// </summary>
        internal DeviceRegistrationResult()
        {

        }

        /// <summary>
        ///. Creates an instance of this class.
        /// </summary>
        public DeviceRegistrationResult(
            string registrationId,
            DateTime? createdDateTimeUtc,
            string assignedHub,
            string deviceId,
            ProvisioningRegistrationStatusType status,
            ProvisioningRegistrationSubstatusType substatus,
            string generationId,
            DateTime? lastUpdatedDateTimeUtc,
            int errorCode,
            string errorMessage,
            string etag,
            JRaw jsonPayload)
        {
            RegistrationId = registrationId;
            CreatedDateTimeUtc = createdDateTimeUtc;
            AssignedHub = assignedHub;
            DeviceId = deviceId;
            Status = status;
            Substatus = substatus;
            GenerationId = generationId;
            LastUpdatedDateTimeUtc = lastUpdatedDateTimeUtc;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Etag = etag;
            Payload = jsonPayload;
        }

        /// <summary>
        ///. Creates an instance of this class.
        /// </summary>
        internal DeviceRegistrationResult(
            string registrationId,
            DateTime? createdDateTimeUtc,
            string assignedHub,
            string deviceId,
            ProvisioningRegistrationStatusType status,
            ProvisioningRegistrationSubstatusType substatus,
            string generationId,
            DateTime? lastUpdatedDateTimeUtc,
            int errorCode,
            string errorMessage,
            string etag,
            JRaw jsonPayload,
            TpmRegistrationResult tpm = default,
            X509RegistrationResult x509 = default,
            SymmetricKeyRegistrationResult symmetricKey = default)
            : this(
                  registrationId,
                  createdDateTimeUtc,
                  assignedHub,
                  deviceId,
                  status,
                  substatus,
                  generationId,
                  lastUpdatedDateTimeUtc,
                  errorCode,
                  errorMessage,
                  etag,
                  jsonPayload)
        {
            Tpm = tpm;
            X509 = x509;
            SymmetricKey = symmetricKey;
        }

        /// <summary>
        /// The registration id.
        /// </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; internal set; }

        /// <summary>
        /// The time when the device originally registered with the service.
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc")]
        public DateTime? CreatedDateTimeUtc { get; internal set; }

        /// <summary>
        /// The assigned Azure IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "assignedHub")]
        public string AssignedHub { get; internal set; }

        /// <summary>
        /// The Device Id.
        /// </summary>
        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; internal set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public ProvisioningRegistrationStatusType Status { get; internal set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        [JsonProperty(PropertyName = "substatus")]
        public ProvisioningRegistrationSubstatusType Substatus { get; internal set; }

        /// <summary>
        /// The generation Id.
        /// </summary>
        [JsonProperty(PropertyName = "generationId")]
        public string GenerationId { get; internal set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc")]
        public DateTime? LastUpdatedDateTimeUtc { get; internal set; }

        /// <summary>
        /// Error code.
        /// </summary>
        [JsonProperty(PropertyName = "errorCode")]
        public int? ErrorCode { get; internal set; }

        /// <summary>
        /// Error message.
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; internal set; }

        /// <summary>
        /// The Etag.
        /// </summary>
        [JsonProperty(PropertyName = "etag")]
        public string Etag { get; internal set; }

        /// <summary>
        /// The custom data returned from the webhook to the device.
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public JRaw Payload { get; internal set; }

        /// <summary>
        /// The registration result for TPM authentication.
        /// </summary>
        [JsonProperty(PropertyName = "tpm")]
        internal TpmRegistrationResult Tpm { get; set; }

        /// <summary>
        /// The registration result for X.509 certificate authentication.
        /// </summary>
        [JsonProperty(PropertyName = "x509")]
        internal X509RegistrationResult X509 { get; set; }

        /// <summary>
        /// The registration result for symmetric key authentication.
        /// </summary>
        [JsonProperty(PropertyName = "symmetricKey")]
        internal SymmetricKeyRegistrationResult SymmetricKey { get; set; }
    }
}
