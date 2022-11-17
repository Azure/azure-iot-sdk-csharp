// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.Provisioning.Service.SharedAccessSignature;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal class ServiceConnectionStringParser
    {
        internal static ServiceConnectionString Parse(string serviceConnectionString)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(serviceConnectionString), "Provisioning service connection string is required.");

            IDictionary<string, string> map = ToDictionary(
                serviceConnectionString, 
                ServiceConnectionStringConstants.ValuePairDelimiter, 
                ServiceConnectionStringConstants.ValuePairSeparator);

            string hostName = GetConnectionStringValue(map, ServiceConnectionStringConstants.HostNamePropertyName);
            string sharedAccessKeyName = GetConnectionStringOptionalValue(map, ServiceConnectionStringConstants.SharedAccessKeyNamePropertyName);
            string sharedAccessKey = GetConnectionStringOptionalValue(map, ServiceConnectionStringConstants.SharedAccessKeyPropertyName);
            string sharedAccessSignature = GetConnectionStringOptionalValue(map, ServiceConnectionStringConstants.SharedAccessSignaturePropertyName);
            string serviceName = GetServiceName(hostName);

            Validate(sharedAccessKeyName, sharedAccessKey, sharedAccessSignature, serviceName);

            return new ServiceConnectionString(hostName, sharedAccessKeyName, sharedAccessKey, sharedAccessSignature, serviceName);
        }

        private static IDictionary<string, string> ToDictionary(string valuePairString, char kvpDelimiter, char kvpSeparator)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(valuePairString), $"{nameof(valuePairString)} cannot be null or white space.");

            IEnumerable<string[]> parts = valuePairString
                .Split(kvpDelimiter)
                .Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Malformed Token.");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            return map.TryGetValue(propertyName, out string value)
                ? value
                : throw new FormatException(
                    $"The connection string is missing the property: {propertyName}.");
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            map.TryGetValue(propertyName, out string value);
            return value;
        }

        private static void Validate(string sharedAccessKeyName, string sharedAccessKey, string sharedAccessSignature, string serviceName)
        {
            if (string.IsNullOrWhiteSpace(sharedAccessKeyName))
            {
                throw new FormatException("Should specify SharedAccessKeyName.");
            }

            if (!(string.IsNullOrWhiteSpace(sharedAccessKey) ^ string.IsNullOrWhiteSpace(sharedAccessSignature)))
            {
                throw new FormatException("Should specify either SharedAccessKey or SharedAccessSignature.");
            }

            if (!string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                // Validate the provided value.
                Convert.FromBase64String(sharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(sharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(serviceName, sharedAccessSignature);
            }
        }

        private static string GetServiceName(string hostName)
        {
            int index = hostName.IndexOf(ServiceConnectionStringConstants.HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string serviceName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return serviceName;
        }
    }
}
