// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    /// <summary>
    /// Exception thrown when IoT Hub exceeds the available quota for active jobs
    /// </summary>
    [Serializable]
    public sealed class JobQuotaExceededException : IotHubException
    {
        private const string JobQuotaExceededMessage = "Job quota has been exceeded";

        /// <summary>
        /// Initializes an instance of JobQuotaExceededException with the default message
        /// </summary>
        public JobQuotaExceededException()
            : base(JobQuotaExceededMessage)
        {
        }

        /// <summary>
        /// Initializes an instance of JobQuotaExceededException with the message from the Http response filled in
        /// </summary>
        /// <param name="message">The error message returned in the Http response</param>
        public JobQuotaExceededException(string message)
            : base(message)
        {
        }
    }
}
