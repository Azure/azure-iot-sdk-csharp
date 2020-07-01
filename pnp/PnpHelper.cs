// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Azure.Devices.Client;

namespace PnpHelpers
{
    public static class PnpHelper
    {
        public const string TelemetryComponentPropertyName = "$.sub";
        private const string EncodingUtf8 = "utf-8";
        private const string ContentApplicationJson = "application/json";
        private const string PropertyComponentIdentifier = "\"__t\": \"c\",";

        public static string CreateTelemetryPayload(string telemetryName, object telemetryValue)
        {
            return $"{{ \"{telemetryName}\": {telemetryValue} }}";
        }

        public static Message CreateIothubMessageUtf8(string telemetryName, object telemetryValue, string componentName = null)
        {
            string payload = $"{{ \"{telemetryName}\": {telemetryValue} }}";
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

        public static string CreateReportedPropertiesPatch(string propertyName, object propertyValue, string componentName = null)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return $"" +
                    $"{{" +
                    $"  \"{propertyName}\": " +
                    $"      {{ " +
                    $"          \"value\": \"{propertyValue}\" " +
                    $"      }} " +
                    $"}}";
            }

            return $"" +
                $"{{" +
                $"  \"{componentName}\": " +
                $"      {{" +
                $"          {PropertyComponentIdentifier}" +
                $"          \"value\": \"{propertyValue}\" " +
                $"      }} " +
                $"}}";
        }
    }
}
