// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the queried job details are not available on IoT Hub.
    /// </summary>
    [Serializable]
    public sealed class JobNotFoundException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="JobNotFoundException"/> with the Id of the job and marks it as non-transient.
        /// </summary>
        /// <param name="jobId">The Id of the job whose details are unavailable on IoT Hub.</param>
        public JobNotFoundException(string jobId)
            : base($"Job with Id '{jobId}' not found")
        {
        }

        internal JobNotFoundException()
            : base()
        {
        }

        internal JobNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private JobNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
