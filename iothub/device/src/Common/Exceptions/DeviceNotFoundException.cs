// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception is thrown when the device is disabled and will be used to set the state to device disabled in the
    /// connection state handler. This exception also corresponds to the following error codes on operation responses:
    /// <list>
    /// <item>AmqpErrorCode.NotFound</item>
    /// <item>HttpStatusCode.NotFound</item>
    /// <item>HttpStatusCode.NoContent</item>
    /// </list>
    /// </summary>
    [Serializable]
    public sealed class DeviceNotFoundException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DeviceNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the device identifier that could not be found.
        /// </summary>
        /// <param name="deviceId">Device identifier that already exists.</param>
        public DeviceNotFoundException(string deviceId)
            : this(deviceId, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the device identifier and the IoT hub instance that could not be found.
        /// </summary>
        /// <param name="deviceId">Device identifier that already exists.</param>
        /// <param name="iotHubName">Name of the IoT hub instance.</param>
        public DeviceNotFoundException(string deviceId, string iotHubName)
            : this(deviceId, iotHubName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the device identifier that could not be found.
        /// </summary>
        /// <param name="deviceId">Device identifier that already exists.</param>
        /// <param name="iotHubName">Name of the IoT hub instance.</param>
        /// <param name="trackingId">Tracking identifier that is used for telemetry purposes.</param>
        public DeviceNotFoundException(string deviceId, string iotHubName, string trackingId)
            : base(
                !string.IsNullOrEmpty(iotHubName)
                      ? "Device {0} at IotHub {1} not registered".FormatInvariant(deviceId, iotHubName)
                      : "Device {0} not registered".FormatInvariant(deviceId),
                null,
                false,
                trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string containing the identifier of the device that could not be found and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public DeviceNotFoundException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }

#pragma warning disable CA2229 // Implement serialization constructors. Would change public API.

        /// <summary>
        /// Initializes a new instance of the class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        public DeviceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#pragma warning restore CA2229 // Implement serialization constructors
    }
}
