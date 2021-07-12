// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// Error codes for common IoT hub exceptions.
    /// </summary>
    public enum ErrorCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Used when the error code returned by the hub is unrecognized. If encountered, please report the issue so it can be added here.
        /// </summary>
        InvalidErrorCode = 0,

        // BadRequest - 400

        /// <summary>
        /// The API version used by the SDK is not supported by the IoT hub endpoint used in this connection.
        /// Usually this would mean that the region of the hub doesn't yet support the API version. One should
        /// consider downgrading to a previous version of the SDK that uses an older API version, or use a hub
        /// in a region that supports it.
        /// </summary>
        InvalidProtocolVersion = 400001,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DeviceInvalidResultCount = 400002,

        InvalidOperation = 400003,
        ArgumentInvalid = 400004,
        ArgumentNull = 400005,
        IotHubFormatError = 400006,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DeviceStorageEntitySerializationError = 400007,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        BlobContainerValidationError = 400008,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        ImportWarningExistsError = 400009,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        InvalidSchemaVersion = 400010,

        /// <summary>
        /// A devices with the same Id was present multiple times in the input request for bulk device registry operations.
        /// <para>
        /// For more information <see href="https://docs.microsoft.com/rest/api/iothub/service/bulk-registry/update-registry"/> on bulk registry operations.
        /// </para>
        /// </summary>
        DeviceDefinedMultipleTimes = 400011,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DeserializationError = 400012,

        /// <summary>
        /// An error was encountered processing bulk registry operations.
        /// <para>
        /// As this error is in the 4xx HTTP status code range, the service would have detected a problem with the job
        /// request or user input.
        /// </para>
        /// </summary>
        //[Obsolete("This error does not appear to be returned by the service.")]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        BulkRegistryOperationFailure = 400013,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        CannotRegisterModuleToModule = 400301,

        // Unauthorized - 401
        IotHubNotFound = 401001,

        /// <summary>
        /// The SAS token has expired or IoT hub couldn't authenticate the authentication header, rule, or key.
        /// For detailed information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized"/>.
        /// </summary>
        IotHubUnauthorizedAccess = 401002,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK. Replaced by <see cref="IotHubUnauthorizedAccess"/>
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IotHubUnauthorized = 401003,

        // Forbidden - 403

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IotHubSuspended = 403001,

        /// <summary>
        /// The daily message quota for the IoT hub is exceeded.
        /// </summary>
        IotHubQuotaExceeded = 403002,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        JobQuotaExceeded = 403003,

        /// <summary>
        /// The underlying cause is that the number of cloud-to-device messages enqueued for the device exceeds the queue limit (50).
        /// You will need to receive and complete/reject the messages from the device-side before you can enqueue any additional messages.
        /// If you want to discard the currently enqueued messages,
        /// you can <see href="https://github.com/Azure/azure-iot-sdk-csharp/blob/c76a64de272da986b6840251f482249e094a725c/iothub/service/src/ServiceClient.cs#L211">purge your device message queue</see>.
        /// </summary>
        DeviceMaximumQueueDepthExceeded = 403004,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IotHubMaxCbsTokenExceeded = 403005,

        // NotFound - 404

        /// <summary>
        /// The operation failed because the device cannot be found by IoT Hub. The device is either not registered or disabled.
        /// </summary>
        DeviceNotFound = 404001,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        JobNotFound = 404002,

        /// <summary>
        /// The error is internal to IoT Hub and is likely transient.
        /// For more details, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-503003-partitionnotfound">503003 PartitionNotFound</see>.
        /// </summary>
        PartitionNotFound = 503003, // We do not handle this error code in our SDK

        ModuleNotFound = 404010,

        // Conflict - 409

        /// <summary>
        /// There's already a device with the same device Id in the IoT hub.
        /// </summary>
        DeviceAlreadyExists = 409001,

        ModuleAlreadyExistsOnDevice = 409301,

        /// <summary>
        /// The etag in the request does not match the etag of the existing resource, as per <see href="https://datatracker.ietf.org/doc/html/rfc7232">RFC7232</see>.
        /// The etag is controlled by the service and is based on the device identity it should not be updated in normal operations.
        /// </summary>
        PreconditionFailed = 412001, // PreconditionFailed - 412

        /// <summary>
        /// When a device receives a cloud-to-device message from the queue (for example, using ReceiveAsync())
        /// the message is locked by IoT hub for a lock timeout duration of one minute.
        /// If the device tries to complete the message after the lock timeout expires, IoT hub throws this exception.
        /// </summary>
        DeviceMessageLockLost = 412002,

        // RequestEntityTooLarge - 413
        /// <summary>
        /// When the message is too large for IoT Hub you will receive this error. You should attempt to reduce your message size and send again.
        /// For more information on message sizes, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits">IoT Hub quotas and throttling | Other limits</see>
        /// </summary>
        MessageTooLarge = 413001,

        TooManyDevices = 413002,
        TooManyModulesOnDevice = 413003,

        // Throttling Exception

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// For more information, <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling">IoT Hub quotas and throttling</see>.
        /// </summary>
        ThrottlingException = 429001,

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// For more information, <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-quotas-throttling">IoT Hub quotas and throttling</see>.
        /// </summary>
        ThrottleBacklogLimitExceeded = 429002,

        /// <summary>
        /// IoT hub ran into a server side issue when attempting to throttle.
        /// For more information, <see href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-troubleshoot-error-500xxx-internal-errors">500xxx Internal errors</see>.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        InvalidThrottleParameter = 500009,

        // InternalServerError - 500

        /// <summary>
        /// IoT hub ran into a server side issue.
        /// There can be a number of causes for a 500xxx error response. In all cases, the issue is most likely transient.
        /// IoT hub nodes can occasionally experience transient faults. When your application tries to connect to a node that is
        /// having issues, you receive this error. To mitigate 500xxx errors, issue a retry from your application.
        /// </summary>
        ServerError = 500001,

        JobCancelled = 500002,

        // ServiceUnavailable

        /// <summary>
        /// IoT hub is currently unable to process the request. This is a transient, retryable error.
        /// </summary>
        ServiceUnavailable = 503001,

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
