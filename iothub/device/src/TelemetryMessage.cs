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
    /// This is a sub class of <see cref="Message"/> designed to accept a <see cref="TelemetryCollection"/> to utilize the <see cref="IPayloadConvention"/> to adhere to a well defined convention. The convention defines the <see cref="ContentType"/> and <see cref="ContentEncoding"/> for this message.
    /// </remarks>
    public class TelemetryMessage : Message
    {
        private TelemetryCollection _telemtryCollection = new TelemetryCollection();
        /// <summary>
        /// Gets or sets the <see cref="TelemetryCollection"/> for this TelemetryMessage
        /// </summary>
        /// <remarks>
        /// Setting this value with a new instance ofd 
        /// </remarks>
        public TelemetryCollection Telemetry
        {
            get => _telemtryCollection;
            set 
            {
                _telemtryCollection = value;
                if (value != null)
                {
                    base.ContentType = value.Convention.PayloadEncoder.ContentEncoding.WebName;
                    base.ContentEncoding = value.Convention.PayloadSerializer.ContentType;
                } else
                {
                    base.ContentType = string.Empty;
                    base.ContentEncoding = string.Empty;
                }
                
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The ability to set this property has been hidden by this class to only allow you to set the ContentType via the <see cref="TelemetryCollection"/> class.
        /// </remarks>
        new public string ContentType
        {
            get => base.ContentType;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The ability to set this property has been hidden by this class to only allow you to set the ContentEncoding via the <see cref="TelemetryCollection"/> class.
        /// </remarks>
        new public string ContentEncoding
        {
            get => base.ContentEncoding;
        }

        /// <summary>
        /// A conveneince constructor that allows you to set the <see cref="TelemetryCollection"/> and <see cref="Message.ComponentName"/> of this <see cref="TelemetryMessage"/>
        /// </summary>
        /// <param name="componentName">The name of the component.</param>
        /// <param name="telemetryCollection">A collection of telemetry for this message.</param>
        public TelemetryMessage(string componentName = default, TelemetryCollection telemetryCollection = default)
            : base(telemetryCollection)
        {
            Telemetry = telemetryCollection ?? new TelemetryCollection();
            if (!string.IsNullOrEmpty(componentName))
            {
                ComponentName = componentName;
            }
        }

        /// <inheritdoc/>
        public override Stream GetBodyStream()
        {
            DisposeBodyStream();
            BodyStream = new MemoryStream(_telemtryCollection.GetPayloadObjectBytes());
            return base.GetBodyStream();
        }
    }
}
