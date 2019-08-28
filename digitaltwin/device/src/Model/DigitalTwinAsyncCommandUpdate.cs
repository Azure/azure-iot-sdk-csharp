// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Azure.Iot.DigitalTwin.Device.Model
{
    /// <summary>
    /// Contains information needed for updating an asynchronous command's status.
    /// </summary>
    public class DigitalTwinAsyncCommandUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinAsyncCommandUpdate"/> class.
        /// </summary>
        /// <param name="name">The name of the command to be updated.</param>
        /// <param name="requestId">The request id of the command to be updated.</param>
        /// <param name="status">The status of the the command to be updated.</param>
        /// <param name="payload">The serialized payload of the the command to be updated.</param>
        public DigitalTwinAsyncCommandUpdate(string name, string requestId, int status, string payload)
        {
            this.Name = name;
            this.Payload = payload;
            this.RequestId = requestId;
            this.Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinAsyncCommandUpdate"/> class.
        /// </summary>
        /// <param name="name">The name of the command to be updated.</param>
        /// <param name="requestId">The request id of the command to be updated.</param>
        /// <param name="status">The status of the the command to be updated.</param>
        public DigitalTwinAsyncCommandUpdate(string name, string requestId, int status)
            : this(name, requestId, status, string.Empty)
        {
        }

        /// <summary>
        /// Gets the command name associated with this update.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the serialized payload associated with this update.
        /// </summary>
        public string Payload { get; private set; }

        /// <summary>
        /// Gets the command request id associated with this update.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Gets the status associated with this update.
        /// </summary>
        public int Status { get; private set; }
    }
}
