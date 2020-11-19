// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The exception that is thrown when an error occurs while processing messages over an MQTT channel.
    /// </summary>
    public class ChannelMessageProcessingException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="ChannelMessageProcessingException"/> with a reference
        /// to the inner exception that caused this exception and the MQTT channel handler context.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">The context associated with the MQTT channel.</param>
        public ChannelMessageProcessingException(Exception innerException, IChannelHandlerContext context)
            : base(string.Empty, innerException)
        {
            Context = context;
        }

        internal ChannelMessageProcessingException()
            : base()
        {
        }

        internal ChannelMessageProcessingException(string message)
            : base(message)
        {
        }

        internal ChannelMessageProcessingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// The context associated with the MQTT channel.
        /// </summary>
        public IChannelHandlerContext Context { get; private set; }
    }
}
