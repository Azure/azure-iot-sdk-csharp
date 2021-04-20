// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The telmetry collection used to populate a <see cref="TelemetryMessage"/>.
    /// </summary>
    public class TelemetryCollection : PayloadCollection
    {

        /// <inheritdoc/>
        public TelemetryCollection(IPayloadConvention payloadConvention = default)
            : base(payloadConvention)
        {
        }

        /// <summary>
        /// Adds the telemetry.
        /// </summary>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryName']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/param['telemetryValue']"/>
        /// <inheritdoc cref="AddOrUpdate(string, object)" path="/exception"/>
        /// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
        public void Add(string telemetryName, object telemetryValue)
        {
            Collection.Add(telemetryName, telemetryValue);
        }

        /// <summary>
        /// Adds or updates the telemetry.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry.</param>
        /// <param name="telemetryValue">The value of the telemetry.</param>
        /// <exception cref="ArgumentNullException"><paramref name="telemetryName"/> is <c>null</c> </exception>
        public void AddOrUpdate(string telemetryName, object telemetryValue)
        {
            Collection[telemetryName] =  telemetryValue;
        }
    }
}
