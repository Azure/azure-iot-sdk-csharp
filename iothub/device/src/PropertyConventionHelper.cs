// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public static class PropertyConventionHelper
    {
        /// <summary>
        /// Create a property collection.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="propertyValue">The unserialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for the properties.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreatePropertyPatch(string propertyName, object propertyValue, string componentName = default, IPayloadConvention payloadConvention = default)
            => CreatePropertiesPatch(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, payloadConvention);

        /// <summary>
        /// Create a property collection.
        /// </summary>
        /// <param name="properties">Reported properties to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <param name="payloadConvention">A convention handler that defines the content encoding and serializer to use for the properties.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreatePropertiesPatch(IDictionary<string, object> properties, string componentName = default, IPayloadConvention payloadConvention = default)
        {
            payloadConvention ??= PropertyConvention.Instance;

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (componentName == null)
            {
                return new PropertyCollection(properties, payloadConvention);
            }

            properties.Add(PropertyConvention.ComponentIdentifierKey, PropertyConvention.ComponentIdentifierValue);
            var componentProperties = new Dictionary<string, object>
                {
                    { componentName, properties }
                };

            return new PropertyCollection(componentProperties, payloadConvention);
        }

        /// <summary>
        /// Create a writable property collection.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="writablePropertyResponse">The writable property response to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <returns>A plug and play compatible writable property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreateWritablePropertyPatch(string propertyName, WritablePropertyResponse writablePropertyResponse, string componentName = default)
            => CreatePropertiesPatch(new Dictionary<string, object> { { propertyName, writablePropertyResponse } }, componentName, PropertyConvention.Instance);
    }
}
