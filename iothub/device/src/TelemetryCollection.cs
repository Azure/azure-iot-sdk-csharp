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
        /// Adds the telemetry to the telemetry collection.
        /// </summary>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryValue']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/exception"/>
        /// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
        public override void Add(string telemetryName, object telemetryValue)
        {
            base.Add(telemetryName, telemetryValue);
        }

        /// <summary>
        /// Adds or updates the telemetry collection.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry.</param>
        /// <param name="telemetryValue">The value of the telemetry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryName"/> is <c>null</c> </exception>
        public override void AddOrUpdate(string telemetryName, object telemetryValue)
        {
            base.AddOrUpdate(telemetryName, telemetryValue);
        }

        /// <summary>
        /// Adds the telemetry values to the telemetry collection.
        /// </summary>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryValue']"/>
        /// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
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

        /// <summary>
        /// Adds or updates the telemetry values in the telemetry collection.
        /// </summary>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryValue']"/>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryValues"/> is <c>null</c>.</exception>
        public void AddOrUpdate(IDictionary<string, object> telemetryValues)
        {
            if (telemetryValues == null)
            {
                throw new ArgumentNullException(nameof(telemetryValues));
            }

            telemetryValues
                .ToList()
                .ForEach(entry => base.AddOrUpdate(entry.Key, entry.Value));
        }
    }
}
