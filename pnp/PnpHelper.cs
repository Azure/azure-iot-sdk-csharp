// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace PnpHelpers
{
    public static class PnpHelper
    {
        private const string TelemetryComponentPropertyName = "$.sub";
        private const string EncodingUtf8 = "utf-8";
        private const string ContentApplicationJson = "application/json";
        private const string PropertyComponentIdentifierKey = "__t";
        private const string PropertyComponentIdentifierValue = "c";

        /// <summary>
        /// Create a plug and play compatible telemetry message.
        /// </summary>
        /// <param name="telemetryName">The name of the telemetry, as defined in the DTDL interface. Must be 64 characters or less. For more details refer <see href="https://github.com/Azure/opendigitaltwins-dtdl/blob/master/DTDL/v2/dtdlv2.md#telemetry"/>.</param>
        /// <param name="serializedTelemetryValue">The serialized telemetry payload, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">(optional) The name of the component in which the telemetry is defined. Can be null for telemetry defined under the root interface.</param>
        /// <returns>A plug and play compatible telemetry message, which can be sent to IoT Hub.</returns>
        public static Message CreateIothubMessageUtf8(string telemetryName, string serializedTelemetryValue, string componentName = default)
        {
            string payload = $"{{ \"{telemetryName}\": {serializedTelemetryValue} }}";

            var message = new Message(Encoding.UTF8.GetBytes(payload))
            {
                ContentEncoding = EncodingUtf8,
                ContentType = ContentApplicationJson,
            };

            if (!string.IsNullOrWhiteSpace(componentName))
            {
                message.Properties.Add(TelemetryComponentPropertyName, componentName);
            }

            return message;
        }

        /// <summary>
        /// Create a key-value property patch for both read-only and read-write properties.
        /// </summary>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="serializedPropertyValue">The serialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="componentName">(optional) The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>The property patch for read-only and read-write property updates.</returns>
        public static string CreatePropertyPatch(string propertyName, string serializedPropertyValue, string componentName = default)
        {
            return string.IsNullOrWhiteSpace(componentName)
                ?
                    $"{{" +
                    $"  \"{propertyName}\": {serializedPropertyValue}" +
                    $"}}"
                :
                    $"{{" +
                    $"  \"{componentName}\": " +
                    $"      {{" +
                    $"          \"{PropertyComponentIdentifierKey}\": \"{PropertyComponentIdentifierValue}\"," +
                    $"          \"{propertyName}\": {serializedPropertyValue}" +
                    $"      }} " +
                    $"}}";
        }

        /// <summary>
        /// Create a key-embedded value property patch for read-write properties.
        /// Embedded value property updates are sent from a device in response to a service-initiated read-write property update.
        /// </summary>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="serializedPropertyValue">The serialized property value, in the format defined in the DTDL interface.</param>
        /// <param name="ackCode">The acknowledgment code from the device, for the embedded value property update.</param>
        /// <param name="ackVersion">The version no. of the service-initiated read-write property update.</param>
        /// <param name="serializedAckDescription">The serialized description from the device, accompanying the embedded value property update.</param>
        /// <param name="componentName">(optional) The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>The property patch for embedded value property updates for read-write properties.</returns>
        public static string CreatePropertyEmbeddedValuePatch(
            string propertyName,
            string serializedPropertyValue,
            int ackCode,
            long ackVersion,
            string serializedAckDescription = default,
            string componentName = default)
        {
            return string.IsNullOrWhiteSpace(componentName)
                ?
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\" : {serializedPropertyValue}," +
                    $"          \"ac\" : {ackCode}, " +
                    $"          \"av\" : {ackVersion}, " +
                    $"          {(!string.IsNullOrWhiteSpace(serializedAckDescription) ? $"\"ad\": {serializedAckDescription}" : "")}" +
                    $"      }} " +
                    $"}}"
                :
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
                    $"}}";
        }

        /// <summary>
        /// Helper to retrieve the required property value from the <see cref="TwinCollection"/> property update patch received from service-initiated update.
        /// </summary>
        /// <typeparam name="T">The data type of the required property, as defined in the DTDL interface.</typeparam>
        /// <param name="collection">The <see cref="TwinCollection"/> property update patch received as a result of service-initiated update.</param>
        /// <param name="propertyName">The property name, as defined in the DTDL interface.</param>
        /// <param name="componentName">(optional) The name of the component in which the property is defined. Can be null for property defined under the root interface.</param>
        /// <returns>A tuple indicating if the <see cref="TwinCollection"/> property update patch contains the required property update, and the corresponding property value.</returns>
        public static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName, string componentName = null)
        {
            // If the desired property update is for a root component or nested component, verify that property patch received contains the desired property update.
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
            }

            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                {
                    return componentProperty.ContainsKey(propertyName) ? (true, componentProperty.Value<T>(propertyName)) : (false, default);
                }
            }

            return (false, default);
        }

        /// <summary>
        /// Create a plug and play compatible command name.
        /// </summary>
        /// <param name="commandName">The name of the command to be invoked.</param>
        /// <param name="componentName">(optional) The name of the component from which the command is to be invoked. Can be null for command defined under the root interface.</param>
        /// <returns>A plug and play compatible command name.</returns>
        public static string CreatePnpCommandName(string commandName, string componentName = default)
        {
            return string.IsNullOrWhiteSpace(componentName) ? commandName : $"{componentName}*{commandName}";
        }

        /// <summary>
        /// Helper to remove extra whitespace from the supplied string.
        /// </summary>
        /// <param name="input">The string to be formatted.</param>
        /// <returns>The input string, with all whitespace removed.</returns>
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
