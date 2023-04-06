// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The context provided to the error processor for a connection loss event or other failure 
    /// when using the <see cref="MessageFeedbackProcessorClient"/>.
    /// </summary>
    public class MessageFeedbackProcessorError : ErrorContext
    {
        internal MessageFeedbackProcessorError(Exception exception) : base(exception)
        {
        }
    }
}
