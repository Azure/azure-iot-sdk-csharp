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
        {
            RegistrationId = registrationId;
            CreatedDateTimeUtc = createdDateTimeUtc;
            AssignedHub = assignedHub;
            DeviceId = deviceId;
            Status = status;
            GenerationId = generationId;
            LastUpdatedDateTimeUtc = lastUpdatedDateTimeUtc;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Etag = etag;
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
        /// The Device ID.
        /// </summary>
        public string DeviceId { get; protected set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public ProvisioningRegistrationStatusType Status { get; protected set; }

        /// <summary>
        /// The generation ID.
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
    }
}
