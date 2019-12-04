// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Azure.IoT.DigitalTwin.Device.Exceptions
{
    /// <summary>
    /// This exception is thrown when the digital twin device interface is not registered.
    /// </summary>
    public class DigitalTwinDeviceInterfaceNotRegisteredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinDeviceInterfaceNotRegisteredException"/> class.
        /// </summary>
        public DigitalTwinDeviceInterfaceNotRegisteredException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinDeviceInterfaceNotRegisteredException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DigitalTwinDeviceInterfaceNotRegisteredException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinDeviceInterfaceNotRegisteredException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DigitalTwinDeviceInterfaceNotRegisteredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
