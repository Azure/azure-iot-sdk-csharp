// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Authentication
{
    internal class IotHubConnectionStringParser
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNameSeparator = ".";
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

            // Hostname
            string hostName = GetConnectionStringValue(map, HostNamePropertyName);
            if (hostName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("IoT hub hostname must be specified in connection string");
            }

            // Device Id
            string deviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("DeviceId must be specified in connection string");
            }

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
                if (SharedAccessSignature.IsSharedAccessSignature(sharedAccessSignature))
                {
                    SharedAccessSignature.Parse(hostName, sharedAccessSignature);
                }
                else
                {
                    throw new ArgumentException("Invalid shared access signature (SAS).");
                }
            }

            return new IotHubConnectionString
            {
                HostName = hostName,
                GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName),
                DeviceId = deviceId,
                ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName),
                SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName),
                SharedAccessKey = sharedAccessKey,
                SharedAccessSignature = sharedAccessSignature,
            };
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
