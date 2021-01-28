﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// Error Codes for common IoT hub exceptions.
    /// </summary>
    public enum ErrorCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        InvalidErrorCode = 0,

        // BadRequest - 400
        InvalidProtocolVersion = 400001,

        DeviceInvalidResultCount = 400002,
        InvalidOperation = 400003,
        ArgumentInvalid = 400004,
        ArgumentNull = 400005,
        IotHubFormatError = 400006,
        DeviceStorageEntitySerializationError = 400007,
        BlobContainerValidationError = 400008,
        ImportWarningExistsError = 400009,
        InvalidSchemaVersion = 400010,
        DeviceDefinedMultipleTimes = 400011,
        DeserializationError = 400012,
        BulkRegistryOperationFailure = 400013,
        CannotRegisterModuleToModule = 400301,

        // Unauthorized - 401
        IotHubNotFound = 401001,

        IotHubUnauthorizedAccess = 401002,

        /// <summary>
        /// The SAS token has expired or IoT hub couldn't authenticate the authentication header, rule, or key.
        /// </summary>
        IotHubUnauthorized = 401003,

        // Forbidden - 403
        IotHubSuspended = 403001,

        /// <summary>
        /// The daily message quota for the IoT hub is exceeded.
        /// </summary>
        IotHubQuotaExceeded = 403002,

        JobQuotaExceeded = 403003,

        /// <summary>
        /// The underlying cause is that the number of messages enqueued for the device exceeds the queue limit (50).
        /// The most likely reason that you're running into this limit is because you're using HTTPS to receive the message,
        /// which leads to continuous polling using ReceiveAsync, resulting in IoT hub throttling the request.
        /// </summary>
        DeviceMaximumQueueDepthExceeded = 403004,

        IotHubMaxCbsTokenExceeded = 403005,

        // NotFound - 404

        /// <summary>
        /// The operation failed because the device cannot be found by IoT Hub. The device is either not registered or disabled.
        /// </summary>
        DeviceNotFound = 404001,

        JobNotFound = 404002,
        PartitionNotFound = 404003,
        ModuleNotFound = 404010,

        // Conflict - 409

        /// <summary>
        /// There's already a device with the same device Id in the IoT hub.
        /// </summary>
        DeviceAlreadyExists = 409001,

        ModuleAlreadyExistsOnDevice = 409301,

        // PreconditionFailed - 412
        PreconditionFailed = 412001,

        /// <summary>
        /// When a device receives a cloud-to-device message from the queue (for example, using ReceiveAsync())
        /// the message is locked by IoT hub for a lock timeout duration of one minute.
        /// If the device tries to complete the message after the lock timeout expires, IoT hub throws this exception.
        /// </summary>
        DeviceMessageLockLost = 412002,

        // RequestEntityTooLarge - 413
        MessageTooLarge = 413001,

        TooManyDevices = 413002,
        TooManyModulesOnDevice = 413003,

        // Throttling Exception

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// For more information, <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling"/>
        /// </summary>
        ThrottlingException = 429001,

        ThrottleBacklogLimitExceeded = 429002,
        InvalidThrottleParameter = 429003,

        // InternalServerError - 500

        /// <summary>
        /// IoT hub ran into a server side issue.
        /// There can be a number of causes for a 500xxx error response. In all cases, the issue is most likely transient.
        /// IoT hub nodes can occasionally experience transient faults. When your device tries to connect to a node that is
        /// having issues, you receive this error. To mitigate 500xxx errors, issue a retry from the device.
        /// </summary>
        ServerError = 500001,

        JobCancelled = 500002,

        // ServiceUnavailable

        /// <summary>
        /// IoT hub encountered an internal error.
        /// </summary>
        ServiceUnavailable = 503001,

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
