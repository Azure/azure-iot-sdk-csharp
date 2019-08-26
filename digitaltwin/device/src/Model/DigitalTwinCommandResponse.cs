// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains response of the command passed from the Digital Twin Interface Client to Digital Twin Client
    /// for further processing (response to service).
    /// </summary>
    public class DigitalTwinCommandResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> class.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        /// <param name="payload">The response data of command execution.</param>
        public DigitalTwinCommandResponse(int status, Memory<byte> payload)
        {
            this.Payload = payload;
            this.Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> class.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        public DigitalTwinCommandResponse(int status)
        {
            this.Status = status;
        }

        /// <summary>
        /// Gets the serialized json representation of the payload in the response.
        /// </summary>
        public Memory<byte> Payload
        {
            get; private set;
        }

        /// <summary>
        /// Gets the status of the response.
        /// </summary>
        public int Status
        {
            get; private set;
        }
    }
}
