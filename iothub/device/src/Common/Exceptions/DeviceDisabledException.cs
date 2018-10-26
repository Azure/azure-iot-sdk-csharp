// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Azure.Devices.Client.Extensions;

    // TODO: #707 - This exception is not thrown by any protocol.

    /// <summary>
    /// The exception that is thrown when the device was disabled from IoT Hub.
    /// </summary>
    [Serializable]
    public sealed class DeviceDisabledException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDisabledException"/> class.
        /// </summary>
        public DeviceDisabledException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDisabledException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public DeviceDisabledException(string deviceId)
            : base("Device {0} is disabled".FormatInvariant(deviceId))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDisabledException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="iotHubName">The IoT Hub name.</param>
        public DeviceDisabledException(string deviceId, string iotHubName)
            : this(deviceId, iotHubName, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDisabledException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="iotHubName">The IoT Hub name.</param>
        /// <param name="trackingId">The Azure IoT service-side Tracking ID in Support Requests.</param>
        public DeviceDisabledException(string deviceId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName) ? "Device {0} at IotHub {1} is disabled".FormatInvariant(deviceId, iotHubName) : "Device {0} is disabled".FormatInvariant(deviceId), trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDisabledException"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DeviceDisabledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !NETSTANDARD1_3
        /// <summary>
        /// Obsolete: This exception is not thrown by the SDK.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public DeviceDisabledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
