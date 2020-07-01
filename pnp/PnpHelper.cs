﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;
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

        public static string CreateTelemetryPayload(string telemetryName, string serializedTelemetryValue)
        {
            return $"{{ \"{telemetryName}\": {serializedTelemetryValue} }}";
        }

        public static Message CreateIothubMessageUtf8(string telemetryName, string serializedTelemetryValue, string componentName = null)
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

        public static string CreateReportedPropertiesPatch(string propertyName, string serializedPropertyValue, string componentName = null)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return $"" +
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\": {serializedPropertyValue}" +
                    $"      }} " +
                    $"}}";
            }

            return $"" +
                $"{{" +
                $"  \"{componentName}\": " +
                $"      {{" +
                $"          \"{PropertyComponentIdentifierKey}\": \"{PropertyComponentIdentifierValue}\"," +
                $"          \"value\": {serializedPropertyValue}" +
                $"      }} " +
                $"}}";
        }

        public static (bool, T) GetPropertyFromTwin<T>(TwinCollection collection, string propertyName, string componentName = null)
        {
            // If the desired property update is for a root component, verify that property patch received contains the desired property update.
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return collection.Contains(propertyName) ? (true, (T)collection[propertyName]) : (false, default);
            }

            // If the desired property update is for a nested component, verify that the property patch received contains the pnp component identifier ("__t": "c"),
            // and also the desired property update.
            if (collection.Contains(componentName))
            {
                JObject componentProperty = collection[componentName];
                if (componentProperty.ContainsKey(PropertyComponentIdentifierKey) && componentProperty.Value<string>(PropertyComponentIdentifierKey).Equals(PropertyComponentIdentifierValue))
                {
                    return componentProperty.ContainsKey(propertyName) ? (true, componentProperty.Value<T>(propertyName)) : (false, default);
                }
            }

            return (false, default);
        }
    }
}
