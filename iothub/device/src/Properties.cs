// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A container for properties.
    /// </summary>
    public class Properties
    {
        private PropertyCollection _readOnlyProperties;

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/>
        /// </summary>
        public Properties()
        {
            Writable = new PropertyCollection();
            _readOnlyProperties = new PropertyCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Properties"/> with the specified collections
        /// </summary>
        /// <param name="writablePropertyCollection">A collection of writable properties returned from IoT Hub</param>
        /// <param name="readOnlyPropertyCollection">A collection of read-only properties returned from IoT Hub</param>
        public Properties(PropertyCollection writablePropertyCollection, PropertyCollection readOnlyPropertyCollection)
        {
            Writable = writablePropertyCollection;
            _readOnlyProperties = readOnlyPropertyCollection;
        }

        /// <summary>
        ///
        /// </summary>
        public PropertyCollection Writable { get; private set; }

        /// <summary>
        /// Get the property from the propeties collection
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public dynamic this[string propertyName]
        {
            get
            {
                return _readOnlyProperties[propertyName];
            }
        }

        /// <summary>
        /// Converts a <see cref="TwinProperties"/> collection to a properties collection
        /// </summary>
        /// <param name="twinProperties">The TwinProperties object to convert</param>
        /// <returns></returns>
        public static Properties FromTwinProperties(TwinProperties twinProperties)
        {
            if (twinProperties == null)
            {
                throw new ArgumentNullException(nameof(twinProperties));
            }

            var writablePropertyCollection = new PropertyCollection();
            foreach (KeyValuePair<string, object> property in twinProperties.Desired)
            {
                writablePropertyCollection.AddPropertyToCollection(property.Key, property.Value);
            }

            var propertyCollection = new PropertyCollection();
            foreach (KeyValuePair<string, object> property in twinProperties.Reported)
            {
                propertyCollection.AddPropertyToCollection(property.Key, property.Value);
            }

            return new Properties(writablePropertyCollection, propertyCollection);
        }
    }
}
