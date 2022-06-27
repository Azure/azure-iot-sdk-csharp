// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message : MessageBase
    {
        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        public Message()
            : base()
        {
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        /// <param name="stream">A stream which will be used as body stream.</param>
        // UWP cannot expose a method with System.IO.Stream in signature. TODO: consider adding an IRandomAccessStream overload
        public Message(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body.
        /// </summary>
        /// <remarks>User should treat the input byte array as immutable when sending the message.</remarks>
        /// <param name="byteArray">A byte array which will be used to form the body stream.</param>
        public Message(byte[] byteArray)
            : base(byteArray)
        {
        }

        /// <summary>
        /// This constructor is only used on the Gateway HTTP path and AMQP SendEventAsync() so that we can clean up the stream.
        /// </summary>
        /// <param name="stream">A stream which will be used as body stream.</param>
        /// <param name="streamDisposalResponsibility">Indicates if the stream passed in should be disposed by the
        /// client library, or by the calling application.</param>
        internal Message(Stream stream, StreamDisposalResponsibility streamDisposalResponsibility)
            : base(stream, streamDisposalResponsibility)
        {
        }

        /// <summary>
        /// Used to specify the content type of the message.
        /// </summary>
        public string ContentType
        {
            get => PayloadContentType;
            set => PayloadContentType = value;
        }

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// </summary>
        public string ContentEncoding
        {
            get => PayloadContentEncoding;
            set => PayloadContentEncoding = value;
        }
    }
}
