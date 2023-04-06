// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The context for a given connection loss event for <see cref="MessageFeedbackProcessorClient"/>,
    /// <see cref="FileUploadNotificationProcessorClient"/>, and <see cref="MessagesClient"/>.
    /// </summary>
    public class ErrorContext
    {
        internal ErrorContext(Exception exception)
        {
            Exception = exception;
        }

        /// <summary>
        /// The exception that caused the error processor execute.
        /// </summary>
        /// <remarks>
        /// This exception is usually either of type <see cref="IOException"/> or 
        /// <see cref="IotHubServiceException"/>.
        /// </remarks>
        public Exception Exception { get; }
    }
}
