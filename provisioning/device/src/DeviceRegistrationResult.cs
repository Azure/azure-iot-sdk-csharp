// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The result of a registration operation.
    /// </summary>
    public class DeviceRegistrationResult
    {
        /// <summary>
        /// Used internally by the SDK to create a new instance of the DeviceRegistrationResult class.
        /// This constructor is exposed to allow serialization and unit testing of applications using this SDK.
        /// </summary>
        public DeviceRegistrationResult(
            string registrationId,
            DateTime? createdDateTimeUtc,
            string assignedHub,
            string deviceId,
            ProvisioningRegistrationStatusType status,
            string generationId,
            DateTime? lastUpdatedDateTimeUtc,
            int errorCode,
            string errorMessage,
            string etag)
            : this(
                  registrationId,
                  createdDateTimeUtc,
                  assignedHub,
                  deviceId,
                  status,
                  ProvisioningRegistrationSubstatusType.InitialAssignment,
                  generationId,
                  lastUpdatedDateTimeUtc,
                  errorCode,
                  errorMessage,
                  etag)
        {
        }

        /// <summary>
        /// Used internally by the SDK to create a new instance of the DeviceRegistrationResult class.
        /// This constructor is exposed to allow serialization and unit testing of applications using this SDK.
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
            string etag)
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
                  null)
        {
        }

        /// <summary>
        /// Used internally by the SDK to create a new instance of the DeviceRegistrationResult class.
        /// This constructor is exposed to allow serialization and unit testing of applications using this SDK.
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
            string returnData)
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
                  returnData,
                  null)
        {
        }

        /// <summary>
        /// Used internally by the SDK to create a new instance of the DeviceRegistrationResult class.
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
            string returnData,
            string issuedClientCertificate)
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
            JsonPayload = returnData;
            IssuedClientCertificate = issuedClientCertificate;
        }

        /// <summary>
        /// The registration id.
        /// </summary>
        public string RegistrationId { get; protected set; }

        /// <summary>
        /// The time when the device originally registered with the service.
        /// </summary>
        public DateTime? CreatedDateTimeUtc { get; protected set; }

        /// <summary>
        /// The assigned Azure IoT Hub.
        /// </summary>
        public string AssignedHub { get; protected set; }

        /// <summary>
        /// The Device Id.
        /// </summary>
        public string DeviceId { get; protected set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public ProvisioningRegistrationStatusType Status { get; protected set; }

        /// <summary>
        /// The substatus of the operation.
        /// </summary>
        public ProvisioningRegistrationSubstatusType Substatus { get; protected set; }

        /// <summary>
        /// The generation Id.
        /// </summary>
        public string GenerationId { get; protected set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        public DateTime? LastUpdatedDateTimeUtc { get; protected set; }

        /// <summary>
        /// Error code.
        /// </summary>
        public int? ErrorCode { get; protected set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; protected set; }

        /// <summary>
        /// The Etag.
        /// </summary>
        public string Etag { get; protected set; }

        /// <summary>
        /// The Custom data returned from the webhook to the device.
        /// </summary>
        public string JsonPayload { get; private set; }

        /// <summary>
        /// The client certificate that was signed by the certificate authority.
        /// This client certificate was used by the device provisioning service to register the enrollment with IoT hub.
        /// The IoT device can then use this returned client certificate along with the private key information to authenticate with IoT hub.
        /// </summary>
        public string IssuedClientCertificate { get; set; }
    }
}
