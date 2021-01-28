﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
            string etag) : this(registrationId, createdDateTimeUtc, assignedHub, deviceId, status, ProvisioningRegistrationSubstatusType.InitialAssignment, generationId, lastUpdatedDateTimeUtc, errorCode, errorMessage, etag)
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
        }

        /// <summary>
        ///. Constructor to allow return data
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
    }
}
