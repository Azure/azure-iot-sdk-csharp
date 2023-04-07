// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The context provided to the error processor for a connection loss event or other failure 
    /// when using the <see cref="MessageFeedbackProcessorClient"/>.
    /// </summary>
    public class MessageFeedbackProcessorError
    {
        internal MessageFeedbackProcessorError(Exception exception)
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
