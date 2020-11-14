﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the list of input modules is too large for an operation.
    /// </summary>
    ///
    [Serializable]
    public sealed class TooManyModulesOnDeviceException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="TooManyModulesOnDeviceException"/> with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TooManyModulesOnDeviceException(string message)
            : this(message, string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="TooManyModulesOnDeviceException"/> with the supplied error message and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public TooManyModulesOnDeviceException(string message, string trackingId)
            : base(message, trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="TooManyModulesOnDeviceException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public TooManyModulesOnDeviceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal TooManyModulesOnDeviceException()
            : base()
        {
        }

        private TooManyModulesOnDeviceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
