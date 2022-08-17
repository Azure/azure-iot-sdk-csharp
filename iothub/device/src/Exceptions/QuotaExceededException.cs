/*// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown by the device client when the daily message quota for the IoT hub is exceeded.
    /// </summary>
    /// <remarks>
    /// To resolve this exception please review the
    /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-troubleshoot-error-403002-iothubquotaexceeded">Troubleshoot Quota Exceeded</see> guide.
    /// </remarks>
    [Serializable]
    public sealed class QuotaExceededException : IotHubClientException
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public QuotaExceededException() : base(isTransient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string has
        /// been localized for the current system culture.</param>
        public QuotaExceededException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the class with the message string set to the message parameter and a
        /// reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">A description of the error. The content of message is intended to be
        /// understood by humans. The caller of this constructor is required to ensure that this string has
        /// been localized for the current system culture.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public QuotaExceededException(string message, Exception innerException)
            : base(message, innerException, isTransient: true)
        {
        }
    }
}
*/