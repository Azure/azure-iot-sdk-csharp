// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnectionStringParser
    {
        internal static IotHubConnectionString Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = ToDictionary(iotHubConnectionString, IotHubConnectionStringConstants.ValuePairDelimiter, IotHubConnectionStringConstants.ValuePairSeparator);

            string hostName = GetConnectionStringValue(map, IotHubConnectionStringConstants.HostNamePropertyName);
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName);
            string sharedAccessKey = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyPropertyName);
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName);

            Validate(hostName, sharedAccessKeyName, sharedAccessKey, sharedAccessSignature);

            return new IotHubConnectionString(hostName, sharedAccessKeyName, sharedAccessKey, sharedAccessSignature);
        }

        private static void Validate(string hostName, string sharedAccessKeyName, string sharedAccessKey, string sharedAccessSignature)
        {
            string iotHubName = GetIotHubName(hostName);
            if (string.IsNullOrWhiteSpace(sharedAccessKeyName))
            {
                throw new ArgumentException("Should specify either SharedAccessKeyName.");
            }

            if (!(string.IsNullOrWhiteSpace(sharedAccessKey) ^ string.IsNullOrWhiteSpace(sharedAccessSignature)))
            {
                throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature.");
            }

            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new FormatException("Missing IoT hub name.");
            }

            if (!string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                Convert.FromBase64String(sharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(sharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(iotHubName, sharedAccessSignature);
            }
        }

        private static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(IotHubConnectionStringConstants.HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0
                ? hostName.Substring(0, index)
                : hostName;
            return iotHubName;
        }

        private static IDictionary<string, string> ToDictionary(string valuePairString, char kvpDelimiter, char kvpSeparator)
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed token");
            }

            IEnumerable<string[]> parts = valuePairString
                .Split(kvpDelimiter)
                .Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new ArgumentException(
                    $"The connection string is missing the property: {propertyName}",
                    nameof(map));
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
