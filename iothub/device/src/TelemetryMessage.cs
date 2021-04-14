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
        private TelemetryCollection _telemtryCollection;

        /// <summary>
        /// 
        /// </summary>
        public TelemetryCollection Telemetry
        {
            get => _telemtryCollection;
            set 
            {
                if (value != null)
                {
                    base.ContentType = value.Convention.PayloadEncoder.ContentEncoding.WebName;
                    base.ContentEncoding = value.Convention.PayloadSerializer.ContentType;
                    base.ComponentName = value.ComponentName;
                } else
                {
                    base.ContentType = string.Empty;
                    base.ContentEncoding = string.Empty;
                    base.ComponentName = string.Empty;
                }
                
            }
        }

        /// <inheritdoc/>
        new public string ComponentName
        {
            get => base.ContentType;
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
        public TelemetryMessage(string componentName)
            : base()
        {
            Telemetry = new TelemetryCollection();
            base.ComponentName = componentName;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="telemetryCollection"></param>
        public TelemetryMessage(TelemetryCollection telemetryCollection = default)
            : base(telemetryCollection)
        {
            Telemetry = telemetryCollection ?? new TelemetryCollection();
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
