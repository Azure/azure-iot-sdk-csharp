// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Authentication
{
    internal sealed class IotHubConnectionStringParser
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNamePropertyName = "HostName";
        private const string GatewayHostNamePropertyName = "GatewayHostName";
        private const string DeviceIdPropertyName = "DeviceId";
        private const string ModuleIdPropertyName = "ModuleId";
        private const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        private const string SharedAccessKeyPropertyName = "SharedAccessKey";
        private const string SharedAccessSignaturePropertyName = "SharedAccessSignature";

        internal static IotHubConnectionString Parse(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            // Host name
            string hostName = GetConnectionStringValue(map, HostNamePropertyName);
            if (hostName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("IoT hub hostname must be specified in connection string");
            }

            // Gateway host name
            string gatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            // Device Id
            string deviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("DeviceId must be specified in connection string");
            }

            // Module Id
            string moduleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);

            // SHared access key name
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);

            // Shared access key
            string sharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            if (!sharedAccessKey.IsNullOrWhiteSpace())
            {
                // Check that the shared access key supplied is a base64 string
                Convert.FromBase64String(sharedAccessKey);
            }

            // Shared access signature
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            if (!sharedAccessSignature.IsNullOrWhiteSpace())
            {
                // Parse the supplied shared access signature string
                // and throw exception if the string is not in the expected format.
                _ = SharedAccessSignatureParser.Parse(sharedAccessSignature);
            }

            return new IotHubConnectionString(
                hostName,
                gatewayHostName,
                deviceId,
                moduleId,
                sharedAccessKeyName,
                sharedAccessKey,
                sharedAccessSignature);
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new ArgumentException($"The connection string is missing the property: {propertyName}.");
            }

            return value;
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            map.TryGetValue(propertyName, out string value);
            return value;
        }
    }
}
