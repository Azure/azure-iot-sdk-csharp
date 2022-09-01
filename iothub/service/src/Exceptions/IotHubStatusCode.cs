// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Exceptions
{
    /// <summary>
    /// The IoT Hub status code.
    /// </summary>
    public enum IotHubStatusCode
    {
        /// <summary>
        /// The request completed without exception.
        /// </summary>
        Ok,

        /// <summary>
        /// The queried configuration is not available on IoT hub.
        /// </summary>
        ConfigurationNotFound,

        /// <summary>
        /// The request fails when an attempt is made to create a device that already exists in the hub.
        /// </summary>
        DeviceAlreadyExists,

        /// <summary>
        /// An attempt to enqueue a message fails because the message queue for the device is already full.
        /// </summary>
        DeviceMaximumQueueDepthExceeded,

        /// <summary>
        /// This status is converted by HttpStatusCode.PreconditionFailed.
        /// </summary>
        DeviceMessageLockLost,

        /// <summary>
        /// The request fails when an attempt is made to access a device instance that is not registered on the IoT hub.
        /// </summary>
        DeviceNotFound,

        /// <summary>
        /// The attempt to communicate with the IoT hub service fails due to transient network issues or operation timeouts.
        /// </summary>
        /// <remarks>
        /// Retrying failed operations could resolve the error.
        /// </remarks>
        NetworkErrors,

        /// <summary>
        /// A request is made against an IoT hub that does not exist.
        /// </summary>
        IotHubNotFound,

        /// <summary>
        /// The request fails because the IoT hub receives an invalid serialization request.
        /// </summary>
        IotHubSerializationFailed,

        /// <summary>
        /// The IoT hub has been suspended.
        /// </summary>
        /// <remarks>
        /// This is likely due to exceeding Azure spending limits. To resolve the error, check the Azure bill and ensure there are enough credits.
        /// </remarks>
        Suspended,

        /// <summary>
        /// Requests to the IoT hub exceed the limits based on the tier of the hub.
        /// </summary>
        /// <remarks>
        /// Retrying with exponential back-off could resolve this error.
        /// For information on the IoT hub quotas and throttling, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling"/>.
        /// </remarks>
        Throttled,

        /// <summary>
        /// The queried job details are not available on IoT hub.
        /// </summary>
        JobNotFound,

        /// <summary>
        /// The IoT hub exceeds the available quota for active jobs.
        /// </summary>
        JobQuotaExceeded,

        /// <summary>
        /// An attempt to send a message fails because the length of the message exceeds the maximum size allowed.
        /// </summary>
        /// <remarks>
        /// When the message is too large for IoT hub you will receive this exception. You should attempt to reduce your
        /// message size and send again. For more information on message sizes, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-quotas-throttling#other-limits">IoT hub
        /// quotas and throttling | Other limits</see>
        /// </remarks>
        MessageTooLarge,

        /// <summary>
        /// An attempt is made to create a module that already exists in the hub.
        /// </summary>
        ModuleAlreadyExists,

        /// <summary>
        /// An attempt is made to access a module instance that is not registered on the IoT hub.
        /// </summary>
        ModuleNotFound,

        /// <summary>
        /// A precondition set by IoT hub is not fulfilled.
        /// </summary>
        PreconditionFailed,

        /// <summary>
        /// The daily message quota for the IoT hub is exceeded.
        /// </summary>
        /// <remarks>
        /// To resolve this exception please review the
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded">Troubleshoot Quota Exceeded</see>
        /// guide.
        /// </remarks>
        MessageQuotaExceeded,

        /// <summary>
        /// The request was rejected by the service because it is too busy to handle it right now.
        /// </summary>
        /// <remarks>
        /// This exception typically means the service is unavailable due to high load or an unexpected error and is usually transient.
        /// The best course of action is to retry your operation after some time.
        /// </remarks>
        ServerBusy,

        /// <summary>
        /// The service encountered an error while handling the request.
        /// </summary>
        /// <remarks>
        /// This exception typically means the IoT hub service has encountered an unexpected error and is usually transient.
        /// Please review the <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-500xxx-internal-errors">500xxx Internal errors</see>
        /// guide for more information. The best course of action is to retry your operation after some time.
        /// </remarks>
        ServerError,

        /// <summary>
        /// The rate of incoming requests exceeds the throttling limit set by IoT hub.
        /// </summary>
        Throttling,

        /// <summary>
        /// The amount of input devices is too large for an operation.
        /// </summary>
        TooManyDevices,

        /// <summary>
        /// The amount of input modules is too large for an operation.
        /// </summary>
        TooManyModulesOnDevice,

        /// <summary>
        /// The request failed because the provided credentials are out of date or incorrect.
        /// </summary>
        /// <remarks>
        /// This exception means the client is not authorized to use the specified IoT hub.
        /// Please review the <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-401003-iothubunauthorized">401003 IoTHubUnauthorized</see>
        /// guide for more information.
        /// </remarks>
        Unauthorized,
    }
}
