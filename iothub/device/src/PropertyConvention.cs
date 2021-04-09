// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class PropertyConvention : ObjectSerializer
    {
        /// <summary>
        ///
        /// </summary>
        public static readonly PropertyConvention Instance = new PropertyConvention();

        /// <summary>
        ///
        /// </summary>
        public static string ComponentIdentifierKey => "__t";

        /// <summary>
        ///
        /// </summary>
        public static string ComponentIdentifierValue => "c";

        /// <summary>
        /// Format a plug and play compatible property payload.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="propertyValue">The unserialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static IDictionary<string, object> FormatPropertyPayload(string propertyName, object propertyValue, string componentName = default)
            => FormatPropertyPayload(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName);

        /// <summary>
        /// Format a plug and play compatible property payload.
        /// </summary>
        /// <param name="properties">Reported properties to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static IDictionary<string, object> FormatPropertyPayload(IDictionary<string, object> properties, string componentName = default)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (componentName == null)
            {
                return properties;
            }

            properties.Add(ComponentIdentifierKey, ComponentIdentifierValue);
            var componentProperties = new Dictionary<string, object>
                {
                    { componentName, properties }
                };

            return componentProperties;
        }

        /// <summary>
        /// Format a plug and play compatible writable property payload.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="writablePropertyResponse">The writable property response to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <returns>A plug and play compatible writable property payload, which can be sent to IoT Hub.</returns>
        public static IDictionary<string, object> FormatWritablePropertyResponsePayload(string propertyName, WritablePropertyResponse writablePropertyResponse, string componentName = default)
            => FormatPropertyPayload(new Dictionary<string, object> { { propertyName, writablePropertyResponse } }, componentName);

        /// <summary>
        /// Create a property collection.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="propertyValue">The unserialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <param name="propertyConvention">A convention handler that defines serializer to use for the properties.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreatePropertyCollection(string propertyName, object propertyValue, string componentName = default, PropertyConvention propertyConvention = default)
            => CreatePropertyCollection(new Dictionary<string, object> { { propertyName, propertyValue } }, componentName, propertyConvention);

        /// <summary>
        /// Create a property collection.
        /// </summary>
        /// <param name="properties">Reported properties to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <param name="propertyConvention">A convention handler that defines serializer to use for the properties.</param>
        /// <returns>A plug and play compatible property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreatePropertyCollection(IDictionary<string, object> properties, string componentName = default, PropertyConvention propertyConvention = default)
        {
            propertyConvention ??= Instance;

            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (componentName == null)
            {
                return new PropertyCollection(properties, propertyConvention);
            }

            properties.Add(ComponentIdentifierKey, ComponentIdentifierValue);
            var componentProperties = new Dictionary<string, object>
                {
                    { componentName, properties }
                };

            return new PropertyCollection(componentProperties, propertyConvention);
        }

        /// <summary>
        /// Create a writable property collection.
        /// </summary>
        /// <param name="propertyName">The name of the property, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#property"/>.</param>
        /// <param name="writablePropertyResponse">The writable property response to push.</param>
        /// <param name="componentName">The component name this property belongs to.</param>
        /// <returns>A plug and play compatible writable property payload, which can be sent to IoT Hub.</returns>
        public static PropertyCollection CreateWritablePropertyCollection(string propertyName, WritablePropertyResponse writablePropertyResponse, string componentName = default)
            => CreatePropertyCollection(new Dictionary<string, object> { { propertyName, writablePropertyResponse } }, componentName, Instance);
    }
}
