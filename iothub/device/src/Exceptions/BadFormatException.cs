// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the request could not be handled by the service because the request
    /// had an unexpected or incorrect format.
    /// </summary>
    [Serializable]
    public sealed class BadFormatException : IotHubException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public BadFormatException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BadFormatException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception instance that caused the current exception.</param>
        public BadFormatException(string message, Exception innerException)
            : base(message, innerException, isTransient: false)
        {
        }
    }
}
