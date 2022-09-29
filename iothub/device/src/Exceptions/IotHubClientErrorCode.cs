// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The IoT hub device/module client error code.
    /// </summary>
    public enum IotHubClientErrorCode
    {
        /// <summary>
        /// The request completed without exception.
        /// </summary>
        Ok,

        /// <summary>
        /// This actually corresponds to QuotaExceeded. For more information on what causes this error
        /// and steps to resolve, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded"/>.
        /// The exception type has not been changed to avoid breaking changes but the inner exception has the correct exception type.
        /// </summary>
        DeviceMaximumQueueDepthExceeded,

        /// <summary>
        /// The request failed because the quota for such operations has been exceeded.
        /// </summary>
        QuotaExceeded,

        /// <summary>
        /// The request failed because attempting to reject/abandon/complete a cloud-to-device message with a lock
        /// token that has already expired. The lock token expires after the lock timeout set by the service, or if your
        /// client connection was lost and regained while receiving the message but before you could reject/abandon/complete it.
        /// </summary>
        /// <remarks>
        /// An abandoned message will be re-enqueued in the per-device/module queue, and the <see cref="IotHubDeviceClient"/> or <see cref="IotHubModuleClient"/> instance will receive it again.
        /// A rejected message will be deleted from the queue and not received again by the device.
        /// For more information on the cause for this error and how to resolve, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-412002-devicemessagelocklost"/>.
        /// For more information on cloud-to-device message lifecycle, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>.
        /// </remarks>
        DeviceMessageLockLost,

        /// <summary>
        /// The request failed because the device is disabled and will be used to set the state to device disabled in the
        /// connection state handler.
        /// </summary>
        DeviceNotFound,

        /// <summary>
        /// The attempt to communicate with the IoT hub service fails due to transient network errors after exhausting
        /// all the retries based on the retry policy set on the client or due to operation timeouts.
        /// </summary>
        /// <remark>
        /// By default, the SDK indefinitely retries dropped connections, unless the retry policy is overridden.
        /// For more information on the SDK's retry policy and how to override it, see <see href="https://github.com/Azure/azure-iot-sdk-csharp/blob/main/iothub/device/devdoc/retrypolicy.md"/>.
        /// </remark>
        NetworkErrors,

        /// <summary>
        /// The IoT hub has been suspended. This is likely due to exceeding Azure spending limits.
        /// </summary>
        /// <remark>
        /// To resolve the error, check the Azure bill and ensure there are enough credits.
        /// </remark>
        Suspended,

        /// <summary>
        /// The request failed because the operation timed out. This can be caused by underlying network issues 
        /// or by the server being too busy to handle the request.
        /// </summary>
        Timeout,

        /// <summary>
        /// The request failed because the IoT hub exceed the limits based on the tier of the hub.
        /// </summary>
        /// <remark>
        /// Retrying with exponential back-off could resolve this error. For information on the IoT hub quotas and throttling, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling"/>.
        /// </remark>
        Throttled,

        /// <summary>
        /// The ETag in the request does not match the ETag of the existing resource.
        /// </summary>
        /// <remark>
        /// The ETag is a mechanism for protecting against the race conditions of multiple clients updating the same resource and overwriting each other.
        /// </remark>
        PreconditionFailed,

        /// <summary>
        /// The attempt to send a message fails because the length of the message exceeds the maximum size allowed.
        /// </summary>
        /// <remarks>
        /// When the message is too large for IoT hub you will receive this exception. You should attempt to reduce
        /// your message size and send again. For more information on message sizes, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits">IoT hub quotas and throttling | Other limits</see>
        /// </remarks>
        MessageTooLarge,

        /// <summary>
        /// The request was rejected by the service because it is too busy to handle it right now.
        /// </summary>
        /// <remarks>
        /// This exception typically means the service is unavailable due to high load or an unexpected error and is usually transient.
        /// The best course of action is to retry your operation after some time.
        /// By default, the SDK will utilize the <see cref="ExponentialBackoff"/> retry strategy.
        /// </remarks>
        ServerBusy,

        /// <summary>
        /// The service encountered an error while handling the request.
        /// </summary>
        /// <remarks>
        /// This exception typically means the IoT hub service has encountered an unexpected error and is usually transient.
        /// Please review the
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-500xxx-internal-errors">500xxx Internal errors</see>
        /// guide for more information. The best course of action is to retry your operation after some time. By default,
        /// the SDK will utilize the <see cref="ExponentialBackoff"/> retry strategy.
        /// </remarks>
        ServerError,

        /// <summary>
        /// The request failed because the provided credentials are out of date or incorrect.
        /// </summary>
        /// <remarks>
        /// This exception means the client is not authorized to use the specified IoT hub. Please review the
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized">401003 IoTHubUnauthorized</see>
        /// guide for more information.
        /// </remarks>
        Unauthorized,
    }
}
