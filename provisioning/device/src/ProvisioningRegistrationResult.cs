// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The result of a registration operation.
    /// </summary>
    public abstract class ProvisioningRegistrationResult
    {
        /// <summary>
        /// The registration id.
        /// </summary>
        public string RegistrationId { get; protected set; }

        /// <summary>
        /// The time when the device originally registered with the service.
        /// </summary>
        public System.DateTime? CreatedDateTimeUtc { get; protected set; }

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
        public ProvisioningRegistrationStatusType Status { get; set; }

        /// <summary>
        /// The generation ID.
        /// </summary>
        public string GenerationId { get; set; }

        /// <summary>
        /// The time when the device last refreshed the registration.
        /// </summary>
        public System.DateTime? LastUpdatedDateTimeUtc { get; set; }

        // TODO: the following APIs may not be required.

        /// <summary>
        /// TODO: Error code.
        /// </summary>
        public int? ErrorCode { get; set; }


        /// <summary>
        /// TODO: Error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// TODO: The Etag.
        /// </summary>
        public string Etag { get; set; }
    }
}
