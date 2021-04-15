// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Devices.Client
{

    /// <summary>
    ///
    /// </summary>
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
        new public string ContentType
        {
            get => base.ContentType;
        }

        /// <inheritdoc/>
        new public string ContentEncoding
        {
            get => base.ContentEncoding;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="telemetryCollection"></param>
        public TelemetryMessage(string componentName = default, TelemetryCollection telemetryCollection = default)
            : base(telemetryCollection)
        {
            Telemetry = telemetryCollection ?? new TelemetryCollection();
            ComponentName = componentName ?? string.Empty;
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
