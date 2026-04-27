// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt is made to access a device instance that is not registered on the IoT hub.
    /// </summary>
    [Serializable]
    public sealed class DeviceNotFoundException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="DeviceNotFoundException"/> with the specified device Id and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device that is not registered on the IoT hub.</param>
        public DeviceNotFoundException(string deviceId)
            : this(deviceId, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceNotFoundException"/> with the specified device Id
        /// and the name of the IoT hub, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device that is not registered on the IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub to which the device should have been registered.</param>
        public DeviceNotFoundException(string deviceId, string iotHubName)
            : this(deviceId, iotHubName, null)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceNotFoundException"/> with the specified device Id,
        /// the name of the IoT hub and the tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device that is not registered on the IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub to which the device should have been registered.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public DeviceNotFoundException(string deviceId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName)
                  ? $"Device {deviceId} at IotHub {iotHubName} not registered."
                  : $"Device {deviceId} not registered.", trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceNotFoundException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="DeviceNotFoundException"/> with a specified <see cref="ErrorCode"/>, error message and an
        /// optional reference to the inner exception that caused this exception. This exception is marked as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="ErrorCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DeviceNotFoundException(ErrorCode code, string message, Exception innerException = null)
            : base(code, message, innerException)
        {
        }

        internal DeviceNotFoundException()
            : base()
        {
        }
    }
}
