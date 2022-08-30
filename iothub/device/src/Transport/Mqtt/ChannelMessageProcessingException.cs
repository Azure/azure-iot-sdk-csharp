// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// The exception that is thrown when an error occurs while processing messages over an MQTT channel.
    /// </summary>
    [Serializable]
    public class ChannelMessageProcessingException : Exception
    {
        /// <summary>
        /// Creates an instance of this class with a reference
        /// to the inner exception that caused this exception and the MQTT channel handler context.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="context">The context associated with the MQTT channel.</param>
        public ChannelMessageProcessingException(Exception innerException, IChannelHandlerContext context)
            : base(string.Empty, innerException)
        {
            Context = context;
        }

        /// <summary>
        /// Creates an instance of the this class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">An object that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">An object that contains contextual information about the source or destination.</param>
        protected ChannelMessageProcessingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
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
