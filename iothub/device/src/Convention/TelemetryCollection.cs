// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The telemetry collection used to populate a <see cref="TelemetryMessage"/>.
    /// </summary>
    public class TelemetryCollection : PayloadCollection
    {
        /// <summary>
        /// Adds or updates the telemetry collection.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry.</param>
        /// <param name="telemetryValue">The value of the telemetry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryName"/> is <c>null</c> </exception>
        public override void Add(string telemetryName, object telemetryValue)
        {
            base.Add(telemetryName, telemetryValue);
        }

        /// <summary>
        /// Adds or updates the telemetry collection.
        /// </summary>
        /// <inheritdoc cref="Add(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="Add(string, object)" path="/param['telemetryValue']"/>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryValues"/> is <c>null</c>.</exception>
        public void Add(IDictionary<string, object> telemetryValues)
        {
            if (telemetryValues == null)
            {
                throw new ArgumentNullException(nameof(telemetryValues));
            }

            telemetryValues
                .ToList()
                .ForEach(entry => base.Add(entry.Key, entry.Value));
        }
    }
}
