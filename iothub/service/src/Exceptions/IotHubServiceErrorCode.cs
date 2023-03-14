// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Error codes for common IoT hub response errors.
    /// </summary>
    public enum IotHubServiceErrorCode
    {
        /// <summary>
        /// Used when the error code returned by the hub is unrecognized. If encountered, please report the issue so it can be added here.
        /// </summary>
        Unknown = 0,

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
        /// Returned by the service if a JSON object provided by this library cannot be parsed, for instance, if the JSON provided for <see cref="TwinsClient.UpdateAsync(string, ClientTwin, bool, System.Threading.CancellationToken)"/> is invalid.
        /// </summary>
        IotHubFormatError = 400006,

        /// <summary>
        /// A devices with the same Id was present multiple times in the input request for bulk device registry operations.
        /// <para>
        /// For more information on bulk registry operations, see <see href="https://docs.microsoft.com/rest/api/iothub/service/bulk-registry/update-registry"/>.
        /// </para>
        /// </summary>
        DeviceDefinedMultipleTimes = 400011,

        /// <summary>
        /// An error was encountered processing bulk registry operations.
        /// <para>
        /// As this error is in the 4xx HTTP status code range, the service would have detected a problem with the job
        /// request or user input.
        /// </para>
        /// </summary>
        BulkRegistryOperationFailure = 400013,

        /// <summary>
        /// The operation failed because the IoT hub has been suspended. 
        /// <para>
        /// This is likely due to exceeding Azure spending limits. To resolve the error, check the Azure bill and ensure there are enough credits.
        /// </para>
        /// </summary>
        IotHubSuspended = 400020,

        // Unauthorized - 401

        /// <summary>
        /// The SAS token has expired or IoT hub couldn't authenticate the authentication header, rule, or key.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized"/>.
        /// </summary>
        IotHubUnauthorizedAccess = 401002,

        // Forbidden - 403

        /// <summary>
        /// Failed to create job since there is another job running.
        /// <para>
        /// Wait and rerun the job after the current job terminates.
        /// </para>
        /// </summary>
        JobQuotaExceeded = 403001,

        /// <summary>
        /// Total number of messages on the hub exceeded the allocated quota.
        /// <para>
        /// Increase units for this hub to increase the quota.
        /// For more information on quota, please refer to <see href="https://aka.ms/iothubthrottling"/>.
        /// </para>
        /// </summary>
        IotHubQuotaExceeded = 403002,

        /// <summary>
        /// The underlying cause is that the number of cloud-to-device messages enqueued for the device exceeds the queue limit.
        /// <para>
        /// You will need to receive and complete/reject the messages from the device-side before you can enqueue any additional messages.
        /// If you want to discard the currently enqueued messages, you can
        /// <see cref="MessagesClient.PurgeMessageQueueAsync(string, System.Threading.CancellationToken)">purge your device message queue</see>.
        /// For more information on cloud-to-device message operations, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
        /// </para>
        /// </summary>
        DeviceMaximumQueueDepthExceeded = 403004,

        // NotFound - 404

        /// <summary>
        /// The operation failed because the device cannot be found by IoT hub.
        /// <para>
        /// The device is either not registered or disabled. May be thrown by operations such as
        /// <see cref="DevicesClient.GetAsync(string, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </summary>
        DeviceNotFound = 404001,

        /// <summary>
        /// The operation failed because the job cannot be found by IoT hub.
        /// </summary>
        JobNotFound = 404002,

        /// <summary>
        /// The operation failed because the module cannot be found by IoT hub.
        /// <para>
        /// The module is either not registered or disabled. May be thrown by operations such as
        /// <see cref="ModulesClient.GetAsync(string, string, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </summary>
        ModuleNotFound = 404010,

        /// <summary>
        /// The operation failed because the requested device isn't online or hasn't registered the direct method callback.
        /// </summary>
        /// <para>
        /// May be thrown by operations such as <see cref="DirectMethodsClient.InvokeAsync(string, DirectMethodServiceRequest, System.Threading.CancellationToken)"/>
        /// </para>
        /// <remarks>
        /// For more information, see <see href="https://docs.microsoft.com/en-us/azure/iot-hub/troubleshoot-error-codes#404103-devicenotonline"/>.
        /// </remarks>
        DeviceNotOnline = 404103,

        // Conflict - 409

        /// <summary>
        /// There's already a device with the same device Id in the IoT hub.
        /// <para>
        /// This can be returned on calling <see cref="DevicesClient.CreateAsync(Device, System.Threading.CancellationToken)"/>
        /// with a device that already exists in the IoT hub.
        /// </para>
        /// </summary>
        DeviceAlreadyExists = 409001,

        /// <summary>
        /// The operation failed because it attempted to add a module to a device when that device already has a module registered to it with the same Id. This issue can be
        /// fixed by removing the existing module from the device first with <see cref="ModulesClient.DeleteAsync(Module, bool, System.Threading.CancellationToken)"/>. This error code is only returned from
        /// methods like <see cref="ModulesClient.CreateAsync(Module, System.Threading.CancellationToken)"/>.
        /// </summary>
        ModuleAlreadyExistsOnDevice = 409301,

        /// <summary>
        /// The ETag in the request does not match the ETag of the existing resource, as per <see href="https://datatracker.ietf.org/doc/html/rfc7232">RFC7232</see>.
        /// <para>
        /// The ETag is a mechanism for protecting against the race conditions of multiple clients updating the same resource and overwriting each other.
        /// In order to get the up-to-date ETag for a twin, see <see cref="TwinsClient.GetAsync(string, System.Threading.CancellationToken)"/> or <see cref="TwinsClient.GetAsync(string, string, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </summary>
        PreconditionFailed = 412001, // PreconditionFailed - 412

        // RequestEntityTooLarge - 413

        /// <summary>
        /// When the message is too large for IoT hub you will receive this error.
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
        /// Too many modules were included in the bulk operation while being added to a device.
        /// </summary>
        TooManyModulesOnDevice = 413003,

        // ThrottlingException Exception

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// <para>
        /// For more information, <see href="https://aka.ms/iothubthrottling">IoT hub quotas and throttling</see>.
        /// </para>
        /// </summary>
        ThrottlingException = 429001,

        /// <summary>
        /// IoT hub throttling limits have been exceeded for the requested operation.
        /// <para>
        /// For more information, <see href="https://aka.ms/iothubthrottling">IoT hub quotas and throttling</see>.
        /// </para>
        /// </summary>
        ThrottlingBacklogTimeout = 429003,

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

        // ServiceUnavailable

        /// <summary>
        /// IoT hub is currently unable to process the request. This is a transient, retryable error.
        /// </summary>
        ServiceUnavailable = 503001,
    }
}
