// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class TelemetryMessage : Message
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="telemetryCollection"></param>
        public TelemetryMessage(TelemetryCollection telemetryCollection)
            : base(telemetryCollection)
        {
        }
    }
}
