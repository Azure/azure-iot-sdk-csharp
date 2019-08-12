// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Model
{
    public class DigitalTwinCommandRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinCommandRequest"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="requestSchemaData">The request schema data to be sent for the command.</param>
        /// <param name="responseTimeout">The time out for the command response.</param>
        /// <param name="connectionTimeout">The connection time out for the command.</param>
        internal DigitalTwinCommandRequest(string name, object requestSchemaData, TimeSpan? responseTimeout, TimeSpan? connectionTimeout)
        {
            Name = name;
            RequestSchemaData = requestSchemaData;
            ResponseTimeout = responseTimeout;
            ConnectionTimeout = connectionTimeout;
        }

        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The request schema data to be sent for the command.
        /// </summary>
        public object RequestSchemaData { get; private set; }

        /// <summary>
        /// The time out for the command response.
        /// </summary>
        public TimeSpan? ResponseTimeout { get; private set; }

        /// <summary>
        /// The connection time out for the command.
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; private set; }

    }
}
