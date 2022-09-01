// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an attempt is made to access a module instance that is not registered on the IoT hub.
    /// </summary>
    [Serializable]
    public class ModuleNotFoundException : IotHubServiceException
    {
        /// <summary>
        /// Creates an instance of this class with the specified module Id and
        /// the device Id in which the module is contained, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device in which the module is contained.</param>
        /// <param name="moduleId">The Id of the module that is not registered on the IoT hub.</param>
        public ModuleNotFoundException(string deviceId, string moduleId)
            : this(deviceId, moduleId, null, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified module Id,
        /// the device Id in which the module is contained and the name of the IoT hub, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device in which the module is contained.</param>
        /// <param name="moduleId">The Id of the module that is not registered on the IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub to which the device and module should have been registered.</param>
        public ModuleNotFoundException(string deviceId, string moduleId, string iotHubName)
            : this(deviceId, moduleId, iotHubName, null)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified module Id,
        /// the device Id in which the module is contained, the name of the IoT hub and the tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="deviceId">The Id of the device in which the module is contained.</param>
        /// <param name="moduleId">The Id of the module that is not registered on the IoT hub.</param>
        /// <param name="iotHubName">The name of the IoT hub to which the device and module should have been registered.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public ModuleNotFoundException(string deviceId, string moduleId, string iotHubName, string trackingId)
            : base(!string.IsNullOrEmpty(iotHubName)
                  ? $"Module {moduleId} on Device {deviceId} at IotHub {iotHubName} not registered."
                  : $"Module {moduleId} on Device {deviceId} not registered", trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ModuleNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2229:Implement serialization constructors",
            Justification = "Cannot modify public API surface since it will be a breaking change")]
        public ModuleNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal ModuleNotFoundException()
            : base()
        {
        }

        internal ModuleNotFoundException(string message)
            : base(message)
        {
        }
    }
}
