// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class TelemetryCollection : PayloadCollection
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="componentName"></param>
        /// <param name="convention"></param>
        public TelemetryCollection(string componentName = default, IPayloadConvention convention = default)
            : base(convention)
        {
            if (componentName != null)
            {
                ComponentName = componentName;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="telemetryName"></param>
        /// <param name="telemetryValue"></param>
        public void Add(string telemetryName, object telemetryValue)
        {
            Collection.Add(telemetryName, telemetryValue);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return Convention.PayloadSerializer.SerializeToString(Collection);
        }
    }
}
