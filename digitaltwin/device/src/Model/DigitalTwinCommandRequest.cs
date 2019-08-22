// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    public class DigitalTwinCommandRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandRequest"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="requestId"> The server generated identifier passed as part of the command.</param>
        /// <param name="payload"> Payload of the request.</param>
        internal DigitalTwinCommandRequest(string name, string requestId, Memory<byte> payload)
        {
            Name = name;
            RequestId = requestId;
            Payload = payload;
        }

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// A server generated string passed as part of the command.
        /// This is used when sending responses to asynchronous commands to act as a correlation Id and/or for diagnostics purposes
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// The data to be sent for the command.
        /// </summary>
        public Memory<byte> Payload { get; private set; }
    }
}
