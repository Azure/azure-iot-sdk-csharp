// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    public class DigitalTwinCommandResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> class.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        /// <param name="payload">The response data of command execution.</param>
        public DigitalTwinCommandResponse(int status, DigitalTwinValue payload)
        {
            Payload = payload?.Value;
            Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandResponse"/> class.
        /// </summary>
        /// <param name="status">The status of the executed command.</param>
        public DigitalTwinCommandResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// The response value after the command executed.
        /// </summary>
        public object Payload
        {
            get; private set;
        }

        /// <summary>
        /// The status of command executed.
        /// </summary>
        public int Status
        {
            get; private set;
        }
    }
}
