// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.DigitalTwin.Model.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    /// <summary>
    /// The Service Connection String Parser class
    /// </summary>
    public class ServiceConnectionStringParser
    {
        protected const char ValuePairDelimiter = ';';
        protected const char ValuePairSeparator = '=';
        protected const string HostNameSeparator = ".";

        protected const string HostNamePropertyName = "HostName";
        protected const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        protected const string SharedAccessKeyPropertyName = "SharedAccessKey";
        protected const string SharedAccessSignaturePropertyName = "SharedAccessSignature";

        protected static TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        protected static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        protected static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        protected static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        protected static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);

        private string _hostName;

        protected ServiceConnectionStringParser()
        {
        }

        /// <summary>
        /// Factory for new Connection String object.
        /// </summary>
        /// <remarks>
        /// The connection string contains a set of information that uniquely identifies an IoT service.
        ///
        /// A valid connection string shall be in the following format:
        /// <code>
        /// HostName=[repositoryHostName];RepositoryId=[repositoryId];SharedAccessKeyName=[keyName];SharedAccessKey=[keyValue]
        /// HostName=[repositoryHostName];SharedAccessKeyName=[keyName];SharedAccessSignature=[signature]
        /// </code>
        ///
        /// This object parse the connection string providing the artifacts to the <see cref="ServiceConnectionString"/> object.
        /// </remarks>
        /// <param name="serviceConnectionString">the <code>string</code> with the connection string information.</param>
        /// <returns>A <code>ServiceConnectionStringParser</code> object with the parsed connection string.</returns>
        public static ServiceConnectionStringParser Create(string serviceConnectionString)
        {
            if (string.IsNullOrWhiteSpace(serviceConnectionString))
            {
                throw new ArgumentNullException(nameof(serviceConnectionString));
            }

            var serviceConnectionStringParser = new ServiceConnectionStringParser();
            serviceConnectionStringParser.Parse(serviceConnectionString);

            return serviceConnectionStringParser;
        }

        /// <summary>
        /// The Service Client Hostname
        /// </summary>
        public string HostName
        {
            get { return _hostName; }
            set { SetHostName(value); }
        }

        /// <summary>
        /// The Service Access Policy Name
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// The Service Shared Access Key for the specified
        /// access policy
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// The Service Shared Access Signature
        /// </summary>
        public string SharedAccessSignatureString { get; internal set; }

        /// <summary>
        /// The Service Name
        /// </summary>
        public string ServiceName { get; private set; }

        internal ServiceConnectionString ToServiceConnectionString()
        {
            Validate();
            return new ServiceConnectionString(this);
        }

        /// <summary>
        /// Returns the Service Connection string
        /// </summary>
        public override string ToString()
        {
            Validate();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, SharedAccessSignatureString);
            if (stringBuilder.Length > 0)
            {
                // Removing delimiter at end of string
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        protected virtual void Parse(string serviceConnectionString)
        {
            IDictionary<string, string> map = serviceConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignatureString = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            Validate();
        }

        protected virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
                // This is to validate SharedAccessKey is base-64 string
                Convert.FromBase64String(SharedAccessKey);
            }

            if (SharedAccessSignature.IsSharedAccessSignature(SharedAccessSignatureString))
            {
                SharedAccessSignature.Parse(ServiceName, SharedAccessSignatureString);
            }

            ValidateFormat(HostName, HostNamePropertyName, HostNameRegex);
            ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignatureString, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
        }

        private void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, HostNamePropertyName, HostNameRegex);
            _hostName = hostname;
            SetServiceName();
        }

        private void SetServiceName()
        {
            ServiceName = GetServiceName(HostName);

            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name");
            }
        }

        protected static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), nameof(value));
            }
        }

        protected static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ValidateFormat(value, propertyName, regex);
            }
        }

        protected static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The connection string is missing the property: {0}", propertyName),
                    nameof(map));
            }

            return value;
        }

        protected static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            map.TryGetValue(propertyName, out value);
            return value;
        }

        protected static string GetServiceName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string serviceName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return serviceName;
        }
    }
}
