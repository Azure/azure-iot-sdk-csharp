// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains information of the invoked command passed from the Digital Twin Client to Digital Twin Interface Client
    /// for further processing.
    /// </summary>
    public struct DigitalTwinCommandRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandRequest"/> struct.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="requestId"> The server generated identifier passed as part of the command.</param>
        /// <param name="payload"> The serialized json representation of the payload in the request.</param>
        internal DigitalTwinCommandRequest(string name, string requestId, string payload)
        {
            this.Name = name;
            this.RequestId = requestId;
            this.Payload = payload;
        }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the server generated identifier of the command.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// Gets serialized json representation of the payload in the request.
        /// </summary>
        public string Payload { get; }
    }
}
