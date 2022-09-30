// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnectionStringParser
    {
        internal static IotHubConnectionString Parse(string iotHubConnectionString)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(iotHubConnectionString), "IoT Hub connection string is required.");
            IDictionary<string, string> map = ToDictionary(iotHubConnectionString, IotHubConnectionStringConstants.ValuePairDelimiter, IotHubConnectionStringConstants.ValuePairSeparator);

            string hostName = GetConnectionStringValue(map, IotHubConnectionStringConstants.HostNamePropertyName);
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName);
            string sharedAccessKey = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyPropertyName);
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName);

            Validate(hostName, sharedAccessKey, sharedAccessSignature);

            return new IotHubConnectionString(hostName, sharedAccessKeyName, sharedAccessKey, sharedAccessSignature);
        }

        private static void Validate(string hostName, string sharedAccessKey, string sharedAccessSignature)
        {
            string iotHubName = GetIotHubName(hostName);

            if (!(string.IsNullOrWhiteSpace(sharedAccessKey) ^ string.IsNullOrWhiteSpace(sharedAccessSignature)))
            {
                throw new FormatException("Should specify either SharedAccessKey or SharedAccessSignature.");
            }

            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new FormatException("Missing IoT Hub name.");
            }

            if (!string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                // The conversion is to validate the provided value.
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
            Debug.Assert(!string.IsNullOrWhiteSpace(valuePairString), $"{nameof(valuePairString)} cannot be null or white space.");

            IEnumerable<string[]> parts = valuePairString
                .Split(kvpDelimiter)
                .Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Malformed token.");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new FormatException(
                    $"The connection string is missing the property: {propertyName}.");
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
