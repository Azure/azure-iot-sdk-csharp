// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    public class DigitalTwinAsyncCommandUpdate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinAsyncCommandUpdate"/> class.
        /// </summary>
        /// <param name="commandName">The name of the command to be updated.</param>
        /// <param name="requestId">The request id of the command to be updated.</param>
        /// <param name="status">The status of the the command to be updated.</param>
        /// <param name="payload">The status of the the command to be updated.</param>
        public DigitalTwinAsyncCommandUpdate(string commandName, string requestId, int status, DigitalTwinValue payload)
        {
            CommandName = commandName;
            Payload = payload?.Value;
            RequestId = requestId;
            Status = status;
        }

        /// <summary>
        /// The command name which the update is for.
        /// </summary>
        public string CommandName { get; private set; }

        /// <summary>
        /// The response value after the command executed.
        /// </summary>
        public object Payload { get; private set; }

        /// <summary>
        /// The request Id of the command which the update is for.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// The status of command executed.
        /// </summary>
        public int Status { get; private set; }
    }
}
