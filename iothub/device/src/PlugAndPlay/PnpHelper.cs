// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Client.PlugAndPlay
{
    /// <summary>
    /// A helper class for formatting messages and properties as per plug and play convention.
    /// </summary>
    public static class PnpHelper
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
        /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details refer <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="serializedTelemetryValue">The serialized telemetry payload, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <param name="encoding">The character encoding to be used when encoding the message body to bytes. This defaults to utf-8.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub.</returns>
        public static Message CreateMessage(string telemetryName, string serializedTelemetryValue, string componentName = default, Encoding encoding = default)
        {
            telemetryName.ThrowIfNullOrWhiteSpace(nameof(telemetryName));
            serializedTelemetryValue.ThrowIfNullOrWhiteSpace(nameof(serializedTelemetryValue));

            Encoding messageEncoding = encoding ?? Encoding.UTF8;
            string payload = $"{{ \"{telemetryName}\": {serializedTelemetryValue} }}".TrimWhiteSpace();
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
        /// Create a key-value property patch for updating device properties.
        /// </summary>
        /// <remarks>
        /// This creates a property patch for both read-only and read-write properties, both of which are named from a service perspective.
        /// All properties are read-write from a device's perspective.
        /// For a root-level property update, the patch is in the format:
        ///     {
        ///         "samplePropertyName": 20
        ///     }
        ///
        /// For a component-level property update, the patch is in the format:
        ///     {
        ///         "sampleComponentName": {
        ///             "__t": "c",
        ///             "samplePropertyName"": 20
        ///         }
        ///     }
        /// </remarks>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="serializedPropertyValue">The serialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static string CreatePropertyPatch(string propertyName, string serializedPropertyValue, string componentName = default)
        {
            propertyName.ThrowIfNullOrWhiteSpace(nameof(propertyName));
            serializedPropertyValue.ThrowIfNullOrWhiteSpace(nameof(serializedPropertyValue));

            string propertyPatch;
            if (string.IsNullOrWhiteSpace(componentName))
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{propertyName}\": {serializedPropertyValue}" +
                    $"}}";
            }
            else
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{componentName}\": " +
                    $"      {{" +
                    $"          \"{PropertyComponentIdentifierKey}\": \"{PropertyComponentIdentifierValue}\"," +
                    $"          \"{propertyName}\": {serializedPropertyValue}" +
                    $"      }} " +
                    $"}}";
            }

            return propertyPatch.TrimWhiteSpace();
        }

        /// <summary>
        /// Create a key-embedded value property patch for updating device properties.
        /// Embedded value property updates are sent from a device in response to a service-initiated read-write property update.
        /// </summary>
        /// A property is either read-only or read-write from a service perspective. All properties are read-write from a device's perspective.
        /// <remarks>
        /// For a root-level property update, the patch is in the format:
        ///     {
        ///         "samplePropertyName": {
        ///             "value": 20,
        ///             "ac": 200,
        ///             "av": 5,
        ///             "ad": "The update was successful."
        ///         }
        ///     }
        ///
        /// For a component-level property update, the patch is in the format:
        ///     {
        ///         "sampleComponentName": {
        ///             "__t": "c",
        ///             "samplePropertyName": {
        ///                 "value": 20,
        ///                 "ac": 200,
        ///                 "av": 5,
        ///                 "ad": "The update was successful."
        ///             }
        ///         }
        ///     }
        /// </remarks>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="serializedPropertyValue">The serialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="ackCode">The acknowledgment code from the device, for the embedded value property update.</param>
        /// <param name="ackVersion">The version no. of the service-initiated read-write property update.</param>
        /// <param name="serializedAckDescription">The serialized description from the device, accompanying the embedded value property update.</param>
        /// <param name="componentName">The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>The property patch for embedded value property updates for read-write properties.</returns>
        public static string CreatePropertyEmbeddedValuePatch(
            string propertyName,
            string serializedPropertyValue,
            int ackCode,
            long ackVersion,
            string serializedAckDescription = default,
            string componentName = default)
        {
            propertyName.ThrowIfNullOrWhiteSpace(nameof(propertyName));
            serializedPropertyValue.ThrowIfNullOrWhiteSpace(nameof(serializedPropertyValue));

            string propertyPatch;
            if (string.IsNullOrWhiteSpace(componentName))
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\" : {serializedPropertyValue}," +
                    $"          \"ac\" : {ackCode}, " +
                    $"          \"av\" : {ackVersion}, " +
                    $"          {(!string.IsNullOrWhiteSpace(serializedAckDescription) ? $"\"ad\": {serializedAckDescription}" : "")}" +
                    $"      }} " +
                    $"}}";
            }
            else
            {
                propertyPatch =
                    $"{{" +
                    $"  \"{componentName}\": " +
                    $"      {{" +
                    $"          \"{PropertyComponentIdentifierKey}\": \"{PropertyComponentIdentifierValue}\"," +
                    $"          \"{propertyName}\": " +
                    $"              {{ " +
                    $"                  \"value\" : {serializedPropertyValue}," +
                    $"                  \"ac\" : {ackCode}, " +
                    $"                  \"av\" : {ackVersion}, " +
                    $"                  {(!string.IsNullOrWhiteSpace(serializedAckDescription) ? $"\"ad\": {serializedAckDescription}" : "")}" +
                    $"              }} " +
                    $"      }} " +
                    $"}}"; ;
            }

            return propertyPatch.TrimWhiteSpace();
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
            collection.ThrowIfNull(nameof(collection));

            // If the desired property update is for a root component or nested component, verify that property patch received contains the desired property update.
            propertyValue = default;

            if (string.IsNullOrWhiteSpace(componentName))
            {
                if (collection.Contains(propertyName))
                {
                    propertyValue = (T)collection[propertyName];
                    return true;
                }
                return false;
            }

            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                {
                    if (componentProperty.ContainsKey(propertyName))
                    {
                        propertyValue = componentProperty.Value<T>(propertyName);
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }
    }
}
