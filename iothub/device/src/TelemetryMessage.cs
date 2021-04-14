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
        private IPayloadConvention _convention;

        /// <inheritdoc/>
        public override string ContentType
        {
            get => base.ContentType;
            set { }
        }

        /// <inheritdoc/>
        public override string ContentEncoding 
        { 
            get => base.ContentEncoding;
            set { } 
        }

        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
        public IDictionary<string, object> Telemetry { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="convention"></param>
        public TelemetryMessage(IPayloadConvention convention = default)
            : base()
        {
            _convention = convention ?? DefaultPayloadConvention.Instance;
            base.ContentEncoding = _convention.PayloadEncoder.ContentEncoding.WebName;
            base.ContentType = _convention.PayloadSerializer.ContentType;
        }
        /// <inheritdoc/>
        public override Stream GetBodyStream()
        {
            BodyStream = new MemoryStream(_convention.GetObjectBytes(this.Telemetry));
            return base.GetBodyStream();
        }
    }
}
