// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class IotHubConnectionStringParser
    {
        internal static IotHubConnectionString Parse(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(IotHubConnectionStringConstants.ValuePairDelimiter, IotHubConnectionStringConstants.ValuePairSeparator);

            string hostName = GetConnectionStringValue(map, IotHubConnectionStringConstants.HostNamePropertyName);
            string gatewayHostName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.GatewayHostNamePropertyName);
            string deviceId = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.DeviceIdPropertyName);
            string moduleId = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.ModuleIdPropertyName);
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName);
            string sharedAccessKey = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyPropertyName);
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName);

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
                throw new ArgumentException($"The connection string is missing the property: {propertyName}.", nameof(propertyName));
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