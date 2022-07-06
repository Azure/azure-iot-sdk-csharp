// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A class used to send telemetry to IoT Hub.
    /// </summary>
    /// <remarks>
    /// This class is derived from <see cref="Message"/> and is designed to accept a <see cref="TelemetryCollection"/>
    /// to utilize the <see cref="PayloadConvention"/> to adhere to a well defined convention.
    /// </remarks>
    public sealed class TelemetryMessage : MessageBase
    {
        /// <summary>
        /// A convenience constructor that allows you to set the <see cref="MessageBase.ComponentName"/>
        /// of this <see cref="TelemetryMessage"/>.
        /// </summary>
        /// <param name="componentName">The name of the component.</param>
        public TelemetryMessage(string componentName = default)
            : base()
        {
            if (!string.IsNullOrEmpty(componentName))
            {
                ComponentName = componentName;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="TelemetryCollection"/> for this <see cref="TelemetryMessage"/>
        /// </summary>
        /// <value>A telemetry collection that will be set as the message payload.</value>
        public TelemetryCollection Telemetry { get; set; } = new TelemetryCollection();

        /// <inheritdoc/>
        public override Stream GetBodyStream()
        {
            DisposeBodyStream();
            BodyStream = new MemoryStream(Telemetry.GetPayloadObjectBytes());
            return base.GetBodyStream();
        }
    }
}
