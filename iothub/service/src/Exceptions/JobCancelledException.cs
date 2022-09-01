// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the queried job has already been canceled on IoT hub.
    /// Note: This exception is currently not thrown by the client library.
    /// </summary>
    [Serializable]
    public sealed class JobCancelledException : IotHubServiceException
    {
        private const string DefaultErrorMessage = "Job has been canceled.";

        /// <summary>
        /// Creates an instance of this class with the default error message and marks it as non-transient.
        /// </summary>
        public JobCancelledException()
            : this(DefaultErrorMessage)
        {
        }

        internal JobCancelledException(string message)
            : base(message)
        {
        }

        internal JobCancelledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
