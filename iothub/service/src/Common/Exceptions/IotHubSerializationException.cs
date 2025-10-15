// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT hub receives an invalid serialization request.
    /// </summary>
    [Serializable]
    public class IotHubSerializationException : IotHubException
    {
        /// <summary>
        /// Creates an instance of this class with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IotHubSerializationException(string message)
            : base(message)
        {
        }

        internal IotHubSerializationException()
            : base()
        {
        }

        internal IotHubSerializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
