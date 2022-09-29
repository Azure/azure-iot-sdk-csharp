// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnectionStringParser
    {
        internal static IotHubConnectionStringBuilder Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = ToDictionary(iotHubConnectionString, IotHubConnectionStringConstants.ValuePairDelimiter, IotHubConnectionStringConstants.ValuePairSeparator);

            string hostName = GetConnectionStringValue(map, IotHubConnectionStringConstants.HostNamePropertyName);
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName);
            string sharedAccessKey = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessKeyPropertyName);
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName);
            string gatewayHostName = GetConnectionStringOptionalValue(map, IotHubConnectionStringConstants.GatewayHostNamePropertyName);

            var iothubConnectionStringBuilder = new IotHubConnectionStringBuilder(hostName, sharedAccessKeyName, sharedAccessKey, sharedAccessSignature, gatewayHostName);
            iothubConnectionStringBuilder.Validate();

            return iothubConnectionStringBuilder;
        }

        /// <summary>
        /// Takes a string representation of key/value pairs and produces a dictionary
        /// </summary>
        /// <param name="valuePairString">The string containing key/value pairs</param>
        /// <param name="kvpDelimiter">The delimeter between key/value pairs</param>
        /// <param name="kvpSeparator">The character separating each key and value</param>
        /// <returns>A dictionary of the key/value pairs</returns>
        internal static IDictionary<string, string> ToDictionary(string valuePairString, char kvpDelimiter, char kvpSeparator)
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

        internal static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new ArgumentException(
                    $"The connection string is missing the property: {propertyName}",
                    nameof(map));
            }

            return value;
        }

        internal static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            map.TryGetValue(propertyName, out string value);
            return value;
        }
    }
}
