// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The collection of twin properties reported by the client.
    /// </summary>
    public class ReportedPropertyCollection : PropertyCollection
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public ReportedPropertyCollection()
        {
        }

        internal ReportedPropertyCollection(Dictionary<string, object> reportedProperties)
            : base(reportedProperties)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <returns></returns>
        public object this[string propertyKey]
        {
            get => _properties[propertyKey];
            set => Add(propertyKey, value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <param name="propertyValue"></param>
        public void Add(string propertyKey, object propertyValue)
        {
            _properties.Add(propertyKey, propertyValue);
        }

        internal byte[] GetObjectBytes()
        {
            return PayloadConvention.GetObjectBytes(_properties);
        }
    }
}
