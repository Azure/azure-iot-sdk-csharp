﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// Error codes for common IoT hub response errors.
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// Used when the error code returned by the hub is unrecognized. If encountered, please report the issue so it can be added here.
        /// </summary>
        InvalidErrorCode = 0,

        // BadRequest - 400

        /// <summary>
        /// The API version used by the SDK is not supported by the IoT hub endpoint used in this connection.
        /// <para>
        /// Usually this would mean that the region of the hub doesn't yet support the API version. One should
        /// consider downgrading to a previous version of the SDK that uses an older API version, or use a hub
        /// in a region that supports it.
        /// </para>
        /// </summary>
        InvalidProtocolVersion = 400001,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DeviceInvalidResultCount = 400002,

        /// <summary>
        /// The client has requested an operation that the hub recognizes as invalid. Check the error message
        /// for more information about what is invalid.
        /// </summary>
        // Note: although infrequent, this does appear in logs for "Amqp Message.Properties.To must contain the device identifier".
        // and perhaps other cases.
        InvalidOperation = 400003,

        /// <summary>
        /// Something in the request payload is invalid. Check the error message for more information about what
        /// is invalid.
        /// </summary>
        // Note: one example found in logs is for invalid characters in a twin property name.
        ArgumentInvalid = 400004,

        /// <summary>
        /// Something in the payload is unexpectedly null. Check the error message for more information about what is invalid.
        /// </summary>
        // Note: an example suggested is null method payloads, but our client converts null to a JSON null, which is allowed.
        ArgumentNull = 400005,

        /// <summary>
        /// Returned by the service if a JSON object provided by this library cannot be parsed, for instance, if the JSON provided for
        /// <see cref="RegistryManager.UpdateTwinAsync(string, Shared.Twin, string)"/> is invalid.
        /// </summary>
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
        /// For more information on bulk registry operations, see <see href="https://docs.microsoft.com/rest/api/iothub/service/bulk-registry/update-registry"/>.
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
        BulkRegistryOperationFailure = 400013,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        CannotRegisterModuleToModule = 400301,

        // Unauthorized - 401

        /// <summary>
        /// The error is internal to IoT hub and is likely transient.
        /// </summary>
        [Obsolete("This error does should not be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IotHubNotFound = 401001,

        /// <summary>
        /// The SAS token has expired or IoT hub couldn't authenticate the authentication header, rule, or key.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized"/>.
        /// </summary>
        IotHubUnauthorizedAccess = 401002,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// Replaced by <see cref="IotHubUnauthorizedAccess"/>
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
        /// Total number of messages on the hub exceeded the allocated quota.
        /// <para>
        /// Increase units for this hub to increase the quota.
        /// For more information on quota, please refer to <see href="https://aka.ms/iothubthrottling"/>.
        /// </para>
        /// </summary>
        IotHubQuotaExceeded = 403002,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        JobQuotaExceeded = 403003,

        /// <summary>
        /// The underlying cause is that the number of cloud-to-device messages enqueued for the device exceeds the queue limit.
        /// <para>
        /// You will need to receive and complete/reject the messages from the device-side before you can enqueue any additional messages.
        /// If you want to discard the currently enqueued messages, you can
        /// <see cref="ServiceClient.PurgeMessageQueueAsync(string, System.Threading.CancellationToken)">purge your device message queue</see>.
        /// For more information on cloud-to-device message operations, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
        /// </para>
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
        /// The operation failed because the device cannot be found by IoT hub.
        /// <para>
        /// The device is either not registered or disabled. May be thrown by operations such as
        /// <see cref="RegistryManager.GetDeviceAsync(string)"/>.
        /// </para>
        /// </summary>
        DeviceNotFound = 404001,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        JobNotFound = 404002,

        /// <summary>
        /// The error is internal to IoT hub and is likely transient.
        /// <para>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-503003-partitionnotfound">503003 PartitionNotFound</see>.
        /// </para>
        /// </summary>
        [Obsolete("This error does should not be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        PartitionNotFound = 503003,

        /// <summary>
        /// The operation failed because the module cannot be found by IoT hub.
        /// <para>
        /// The module is either not registered or disabled. May be thrown by operations such as
        /// <see cref="RegistryManager.GetModuleAsync(string, string)"/>.
        /// </para>
        /// </summary>
        ModuleNotFound = 404010,

        // Conflict - 409

        /// <summary>
        /// There's already a device with the same device Id in the IoT hub.
        /// <para>
        /// This can be returned on calling <see cref="RegistryManager.AddDeviceAsync(Device, System.Threading.CancellationToken)"/>
        /// with a device that already exists in the IoT hub.
        /// </para>
        /// </summary>
        DeviceAlreadyExists = 409001,

        /// <summary>
        /// The operation failed because it attempted to add a module to a device when that device already has a module registered to it with the same Id. This issue can be
        /// fixed by removing the existing module from the device first with <see cref="RegistryManager.RemoveModuleAsync(Module)"/>. This error code is only returned from
        /// methods like <see cref="RegistryManager.AddModuleAsync(Module, System.Threading.CancellationToken)"/>.
        /// </summary>
        ModuleAlreadyExistsOnDevice = 409301,

        /// <summary>
        /// The ETag in the request does not match the ETag of the existing resource, as per <see href="https://datatracker.ietf.org/doc/html/rfc7232">RFC7232</see>.
        /// <para>
        /// The ETag is a mechanism for protecting against the race conditions of multiple clients updating the same resource and overwriting each other.
        /// In order to get the up-to-date ETag for a twin, see <see cref="RegistryManager.GetTwinAsync(string, System.Threading.CancellationToken)"/> or
        /// <see cref="RegistryManager.GetTwinAsync(string, string, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </summary>
        PreconditionFailed = 412001, // PreconditionFailed - 412

        /// <summary>
        /// If the device tries to complete the message after the lock timeout expires, IoT hub throws this exception.
        /// <para>
        /// When a device receives a cloud-to-device message from the queue (for example, using ReceiveAsync())
        /// the message is locked by IoT hub for a lock timeout duration of one minute.
        /// </para>
        /// </summary>
        [Obsolete("This error should not be returned to a service application. This is relevant only for a device application.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        DeviceMessageLockLost = 412002,

        // RequestEntityTooLarge - 413

        /// <summary>
        /// When the message is too large for IoT hub you will receive this error.'
        /// <para>
        /// You should attempt to reduce your message size and send again.
        /// For more information on message sizes, see <see href="https://aka.ms/iothubthrottling#other-limits">IoT hub quotas and throttling | Other limits</see>
        /// </para>
        /// </summary>
        MessageTooLarge = 413001,

        /// <summary>
        /// Too many devices were included in the bulk operation.
        /// <para>
        /// Check the response for details.
        /// For more information, see <see href="https://docs.microsoft.com/rest/api/iothub/service/bulk-registry/update-registry"/>.
        /// </para>
        /// </summary>
        TooManyDevices = 413002,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        TooManyModulesOnDevice = 413003,

        // Throttling Exception

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// For more information, <see href="https://aka.ms/iothubthrottling">IoT hub quotas and throttling</see>.
        /// </summary>
        ThrottlingException = 429001,

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// <para>
        /// For more information, see <see href="https://aka.ms/iothubthrottling">IoT hub quotas and throttling</see>.
        /// </para>
        /// </summary>
        ThrottleBacklogLimitExceeded = 429002,

        /// <summary>
        /// IoT hub ran into a server side issue when attempting to throttle.
        /// <para>
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-500xxx-internal-errors">500xxx Internal errors</see>.
        /// </para>
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        InvalidThrottleParameter = 500009,

        // InternalServerError - 500

        /// <summary>
        /// IoT hub ran into a server side issue.
        /// <para>
        /// There can be a number of causes for a 500xxx error response. In all cases, the issue is most likely transient.
        /// IoT hub nodes can occasionally experience transient faults. When your application tries to connect to a node that is
        /// having issues, you receive this error. To mitigate 500xxx errors, issue a retry from your application.
        /// </para>
        /// </summary>
        ServerError = 500001,

        /// <summary>
        /// Unused error code. Service does not return it and neither does the SDK.
        /// </summary>
        [Obsolete("This error does not appear to be returned by the service.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        JobCancelled = 500002,

        // ServiceUnavailable

        /// <summary>
        /// IoT hub is currently unable to process the request. This is a transient, retryable error.
        /// </summary>
        ServiceUnavailable = 503001,
    }
}
