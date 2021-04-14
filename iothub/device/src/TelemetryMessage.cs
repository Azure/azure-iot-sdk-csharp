// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Devices.Client
{
    
    /// <summary>
    ///
    /// </summary>
    public class TelemetryMessage : Message
    {
        IPayloadConvention _convention;
        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, object> Telemetry { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="convention"></param>
        public TelemetryMessage(IPayloadConvention convention = default)
            : base()
        {
            _convention = convention ?? DefaultPayloadConvention.Instance;
        }
        /// <inheritdoc/>
        public override Stream GetBodyStream()
        {
            BodyStream = new MemoryStream(_convention.GetObjectBytes(this.Telemetry));
            return base.GetBodyStream();
        }
    }
}
