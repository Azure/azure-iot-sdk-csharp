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
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public object this[string propertyName]
        {
            get
            {
                if (Collection != null)
                {
                    if (Collection.TryGetValue(propertyName, out object returnValue))
                    {
                        return returnValue;
                    }
                    
                }
                return null;
            }
            set
            {
                if (Collection != null)
                {
                    Collection.Add(propertyName, value);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="convention"></param>
        public TelemetryCollection(IPayloadConvention convention = default)
            : base(convention)
        {
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
