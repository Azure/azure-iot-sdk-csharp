#region license
// ==============================================================================
// Microsoft patterns & practices Enterprise Library
// Transient Fault Handling Application Block
// ==============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ==============================================================================
#endregion

namespace Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling
{
    using Microsoft.Azure.Devices.Client.TransientFaultHandling.Properties;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The special type of exception that provides managed exit from a retry loop. The user code can use this
    /// exception to notify the retry policy that no further retry attempts are required.
    /// </summary>
    [Obsolete("You should use cancellation tokens or other means of stoping the retry loop.")]
    public sealed class RetryLimitExceededException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException"/> class with a default error message.
        /// </summary>
        public RetryLimitExceededException()
            : this(Resources.RetryLimitExceeded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RetryLimitExceededException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException"/> class with a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RetryLimitExceededException(Exception innerException)
            : base(innerException != null ? innerException.Message : Resources.RetryLimitExceeded, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RetryLimitExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}