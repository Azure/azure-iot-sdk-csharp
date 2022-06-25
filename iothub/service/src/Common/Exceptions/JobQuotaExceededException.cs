// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when IoT hub exceeds the available quota for active jobs.
    /// </summary>
    [Serializable]
    public sealed class JobQuotaExceededException : IotHubException
    {
        private const string DefaultErrorMessage = "Job quota has been exceeded.";

        /// <summary>
        /// Creates an instance of this class with the default error message and marks it as non-transient.
        /// </summary>
        public JobQuotaExceededException()
            : this(DefaultErrorMessage)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JobQuotaExceededException(string message)
            : base(message)
        {
        }

        internal JobQuotaExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
