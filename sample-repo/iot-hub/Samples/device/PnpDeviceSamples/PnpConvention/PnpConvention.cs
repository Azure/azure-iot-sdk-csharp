// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace PnpHelpers
{
    public class PnpConvention
    {
        /// <summary>
        /// The content type for a plug and play compatible telemetry message.
        /// </summary>
        public const string ContentApplicationJson = "application/json";

        /// <summary>
        /// The key for a component identifier within a property update patch. Corresponding value is <see cref="PropertyComponentIdentifierValue"/>.
        /// </summary>
        public const string PropertyComponentIdentifierKey = "__t";

        /// <summary>
        /// The value for a component identifier within a property update patch. Corresponding key is <see cref="PropertyComponentIdentifierKey"/>.
        /// </summary>
        public const string PropertyComponentIdentifierValue = "c";

        /// <summary>
        /// Create a plug and play compatible telemetry message.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="telemetryValue">The unserialized telemetry payload, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub. The caller must dispose this object when finished.</returns>
        public static Message CreateMessage(string telemetryName, object telemetryValue, string componentName = default, Encoding encoding = default)
        {
            if (string.IsNullOrWhiteSpace(telemetryName))
            {
                throw new ArgumentNullException(nameof(telemetryName));
            }
            if (telemetryValue == null)
            {
                throw new ArgumentNullException(nameof(telemetryValue));
            }

            return CreateMessage(new Dictionary<string, object> { { telemetryName, telemetryValue } }, componentName, encoding);
        }

        /// <summary>
        /// Create a plug and play compatible telemetry message.
        /// </summary>
        /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <param name="telemetryPairs">The unserialized name and value telemetry pairs, as defined in the DTDL interface. Names must be 64 characters or less. For more details see
        /// <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub. The caller must dispose this object when finished.</returns>
        public static Message CreateMessage(IDictionary<string, object> telemetryPairs, string componentName = default, Encoding encoding = default)
        {
            if (telemetryPairs == null)
            {
                throw new ArgumentNullException(nameof(telemetryPairs));
            }

            Encoding messageEncoding = encoding ?? Encoding.UTF8;
            string payload = JsonConvert.SerializeObject(telemetryPairs);
            var message = new Message(messageEncoding.GetBytes(payload))
            {
                ContentEncoding = messageEncoding.WebName,
                ContentType = ContentApplicationJson,
            };

            if (!string.IsNullOrWhiteSpace(componentName))
            {
                message.ComponentName = componentName;
            }

            return message;
        }

        /// <summary>
        /// Creates a batch property update payload for the specified property key/value pairs.
        /// </summary>
        /// <param name="propertyName">The name of the twin property.</param>
        /// <param name="propertyValue">The unserialized value of the twin property.</param>
        /// <returns>A compact payload of the properties to update.</returns>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a root-level property update, the patch is in the format: <c>{ "samplePropertyName": 20 }</c>
        /// </remarks>
        public static TwinCollection CreatePropertyPatch(string propertyName, object propertyValue)
        {
            return CreatePropertyPatch(new Dictionary<string, object> { { propertyName, propertyValue } });
        }

        /// <summary>
        /// Creates a batch property update payload for the specified property key/value pairs
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a root-level property update, the patch is in the format: <c>{ "samplePropertyName": 20 }</c>
        /// </remarks>
        /// <param name="propertyPairs">The twin properties and values to update.</param>
        /// <returns>A compact payload of the properties to update.</returns>
        public static TwinCollection CreatePropertyPatch(IDictionary<string, object> propertyPairs)
        {
            return new TwinCollection(JsonConvert.SerializeObject(propertyPairs));
        }

        /// <summary>
        /// Create a key/value property patch for updating digital twin properties.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName"": 20
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <param name="propertyName">The name of the twin property.</param>
        /// <param name="propertyValue">The unserialized value of the twin property.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static TwinCollection CreateComponentPropertyPatch(string componentName, string propertyName, object propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }
            if (propertyValue == null)
            {
                throw new ArgumentNullException(nameof(propertyValue));
            }

            return CreateComponentPropertyPatch(componentName, new Dictionary<string, object> { { propertyName, propertyValue } });
        }

        /// <summary>
        /// Create a key/value property patch for updating digital twin properties.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName": 20
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <param name="propertyPairs">The property name and an unserialized value, as defined in the DTDL interface.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static TwinCollection CreateComponentPropertyPatch(string componentName, IDictionary<string, object> propertyPairs)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }
            if (propertyPairs == null)
            {
                throw new ArgumentNullException(nameof(propertyPairs));
            }

            var propertyPatch = new StringBuilder();
            propertyPatch.Append("{");
            propertyPatch.Append($"\"{componentName}\":");
            propertyPatch.Append("{");
            propertyPatch.Append($"\"{PropertyComponentIdentifierKey}\":\"{PropertyComponentIdentifierValue}\",");
            foreach (var kvp in propertyPairs)
            {
                propertyPatch.Append($"\"{kvp.Key}\":{JsonConvert.SerializeObject(kvp.Value)},");
            }

            // remove the extra comma
            propertyPatch.Remove(propertyPatch.Length - 1, 1);

            propertyPatch.Append("}}");

            return new TwinCollection(propertyPatch.ToString());
        }

        /// <summary>
        /// Creates a response to a write request on a device property.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a component-level property update, the patch is in the format:
        /// <code>
        /// {
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName": 20
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="propertyName">The name of the property to report.</param>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        /// <returns>A serialized json string response.</returns>
        public static TwinCollection CreateWritablePropertyResponse(
            string propertyName,
            object propertyValue,
            int ackCode,
            long ackVersion,
            string ackDescription = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return CreateWritablePropertyResponse(
                new Dictionary<string, object> { { propertyName, propertyValue } },
                ackCode,
                ackVersion,
                ackDescription);
        }

        /// <summary>
        /// Creates a response to a write request on a device property.
        /// </summary>
        /// <param name="propertyPairs">The name and unserialized value of the property to report.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        /// <returns>A serialized json string response.</returns>
        public static TwinCollection CreateWritablePropertyResponse(
            IDictionary<string, object> propertyPairs,
            int ackCode,
            long ackVersion,
            string ackDescription = null)
        {
            if (propertyPairs == null)
            {
                throw new ArgumentNullException(nameof(propertyPairs));
            }

            var response = new Dictionary<string, WritablePropertyResponse>(propertyPairs.Count);
            foreach (var kvp in propertyPairs)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    throw new ArgumentNullException(nameof(kvp.Key), $"One of the propertyPairs keys was null, empty, or white space.");
                }
                response.Add(kvp.Key, new WritablePropertyResponse(kvp.Value, ackCode, ackVersion, ackDescription));
            }

            return new TwinCollection(JsonConvert.SerializeObject(response));
        }

        /// <summary>
        /// Creates a response to a write request on a device property.
        /// </summary>
        /// <remarks>
        /// For a component-level property update, the patch is in the format:
        /// <code>
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName": {
        ///       "value": 20,
        ///       "ac": 200,
        ///       "av": 5,
        ///       "ad": "The update was successful."
        ///     }
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The component to which the property belongs.</param>
        /// <param name="propertyName">The name of the property to report.</param>
        /// <param name="propertyValue">The unserialized property value.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        /// <returns>A serialized json string response.</returns>
        public static TwinCollection CreateComponentWritablePropertyResponse(
            string componentName,
            string propertyName,
            object propertyValue,
            int ackCode,
            long ackVersion,
            string ackDescription = null)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return CreateComponentWritablePropertyResponse(
                componentName,
                new Dictionary<string, object> { { propertyName, propertyValue } },
                ackCode,
                ackVersion,
                ackDescription);
        }

        /// <summary>
        /// Creates a response to a write request on a device property.
        /// </summary>
        /// <remarks>
        /// For a component-level property update, the patch is in the format:
        /// <code>
        ///   "sampleComponentName": {
        ///     "__t": "c",
        ///     "samplePropertyName": {
        ///       "value": 20,
        ///       "ac": 200,
        ///       "av": 5,
        ///       "ad": "The update was successful."
        ///     }
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="componentName">The component to which the property belongs.</param>
        /// <param name="propertyPairs">The name and unserialized value of the property to report.</param>
        /// <param name="ackCode">The acknowledgement code, usually an HTTP Status Code e.g. 200, 400.</param>
        /// <param name="ackVersion">The acknowledgement version, as supplied in the property update request.</param>
        /// <param name="ackDescription">The acknowledgement description, an optional, human-readable message about the result of the property update.</param>
        /// <returns>A serialized json string response.</returns>
        public static TwinCollection CreateComponentWritablePropertyResponse(
            string componentName,
            IDictionary<string, object> propertyPairs,
            int ackCode,
            long ackVersion,
            string ackDescription = null)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                throw new ArgumentNullException(nameof(componentName));
            }
            if (propertyPairs == null)
            {
                throw new ArgumentNullException(nameof(propertyPairs));
            }

            var propertyPatch = new Dictionary<string, object>
            {
                { PropertyComponentIdentifierKey, PropertyComponentIdentifierValue },
            };
            foreach (var kvp in propertyPairs)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    throw new ArgumentNullException(nameof(kvp.Key), $"One of the propertyPairs keys was null, empty, or white space.");
                }
                propertyPatch.Add(kvp.Key, new WritablePropertyResponse(kvp.Value, ackCode, ackVersion, ackDescription));
            }

            var response = new Dictionary<string, object>
            {
                { componentName, propertyPatch },
            };

            return new TwinCollection(JsonConvert.SerializeObject(response));
        }

        /// <summary>
        /// Helper to retrieve the property value from the <see cref="TwinCollection"/> property update patch which was received as a result of service-initiated update.
        /// </summary>
        /// <typeparam name="T">The data type of the property, as defined in the DTDL interface.</typeparam>
        /// <param name="collection">The <see cref="TwinCollection"/> property update patch received as a result of service-initiated update.</param>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="propertyValue">The corresponding property value.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>A boolean indicating if the <see cref="TwinCollection"/> property update patch received contains the property update.</returns>
        public static bool TryGetPropertyFromTwin<T>(TwinCollection collection, string propertyName, out T propertyValue, string componentName = null)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // If the desired property update is for a root component or nested component, verify that property patch received contains the desired property update.
            propertyValue = default;

            if (string.IsNullOrWhiteSpace(componentName))
            {
                if (collection.Contains(propertyName))
                {
                    propertyValue = (T)collection[propertyName];
                    return true;
                }
            }

            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                if (componentProperty.ContainsKey(propertyName))
                {
                    propertyValue = componentProperty.Value<T>(propertyName);
                    return true;
                }
            }

            return false;
        }
    }
}
