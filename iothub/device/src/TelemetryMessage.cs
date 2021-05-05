// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A class used to send Telmetry to IoT Hub
    /// </summary>
    /// <remarks>
    /// This is a sub class of <see cref="Message"/> designed to accept a <see cref="TelemetryCollection"/> to utilize the <see cref="PayloadConvention"/> to adhere to a well defined convention. The convention defines the <see cref="ContentType"/> and <see cref="ContentEncoding"/> for this message.
    /// </remarks>
    public class TelemetryMessage : Message
    {
        /// <summary>
        /// Gets or sets the <see cref="TelemetryCollection"/> for this <see cref="TelemetryMessage"/>
        /// </summary>
        /// <value>A telemetry collection that will be set as the message payload.</value>
        public TelemetryCollection Telemetry { get; set; } = new TelemetryCollection();

        /// <inheritdoc/>
        /// <remarks>
        /// The ability to set this property has been hidden by this class to only allow you to set the ContentType via the <see cref="PayloadSerializer"/> class.
        /// </remarks>
        /// <value>The base <see cref="Message"/> content type.</value>
        public new string ContentType
        {
            get => base.ContentType;        // TODO - this info is available only after deviceClient.SendTelemetryAsync() has been called.
            internal set => base.ContentType = value;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The ability to set this property has been hidden by this class to only allow you to set the ContentEncoding via the <see cref="PayloadEncoder"/> class.
        /// </remarks>
        /// <value>The base <see cref="Message"/> content encoding.</value>
        public new string ContentEncoding
        {
            get => base.ContentEncoding;        // TODO - this info is available only after deviceClient.SendTelemetryAsync() has been called.
            internal set => base.ContentEncoding = value;
        }

        /// <summary>
        /// A conveneince constructor that allows you to set the <see cref="Message.ComponentName"/> of this <see cref="TelemetryMessage"/>
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

        /// <inheritdoc/>
        public override Stream GetBodyStream()
        {
            DisposeBodyStream();
            BodyStream = new MemoryStream(Telemetry.GetPayloadObjectBytes());
            return base.GetBodyStream();
        }
    }
}
