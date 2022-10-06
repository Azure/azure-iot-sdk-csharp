// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="propertyKey"> The key of the value to get or set. </param>
        public object this[string propertyKey]
        {
            get => _properties[propertyKey];
            set => Add(propertyKey, value);
        }

        /// <summary>
        /// Adds the values to the collection.
        /// </summary>
        /// <param name="propertyKey">The key of the property to add.</param>
        /// <param name="propertyValue">The value of the property to add.</param>
        /// <exception cref="ArgumentException"><paramref name="propertyKey"/> already exists in the collection.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="propertyKey"/> is <c>null</c>.</exception>
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
