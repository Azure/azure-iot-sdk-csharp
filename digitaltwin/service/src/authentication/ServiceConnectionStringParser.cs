﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Azure.IoT.DigitalTwin.Service
{
    /// <summary>
    /// The Service Connection String Parser class
    /// </summary>
    public class ServiceConnectionStringParser
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNameSeparator = ".";

        private const string HostNamePropertyName = nameof(HostName);
        private const string SharedAccessKeyNamePropertyName = nameof(SharedAccessKeyName);
        private const string SharedAccessKeyPropertyName = nameof(SharedAccessKey);
        private const string SharedAccessSignaturePropertyName = nameof(SharedAccessSignatureString);
        private const string RepositoryIdPropertyName = nameof(RepositoryId);

        private static readonly TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);

        private string _hostName;
        private string _serviceName;

        private ServiceConnectionStringParser()
        {
        }

        /// <summary>
        /// Factory for new Connection String object.
        /// </summary>
        /// <remarks>
        /// The connection string contains a set of information that uniquely identify an IoT Service.
        ///
        /// A valid connection string shall be in the following format:
        /// <code>
        /// HostName=[ServiceName];SharedAccessKeyName=[keyName];SharedAccessKey=[Key]
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

            var ServiceConnectionStringParser = new ServiceConnectionStringParser();
            ServiceConnectionStringParser.Parse(serviceConnectionString);

            return ServiceConnectionStringParser;
        }

        /// <summary>
        /// The Provisioning Service Client Hostname
        /// </summary>
        public string HostName
        {
            get { return _hostName; }
            set { SetHostName(value); }
        }

        /// <summary>
        /// The Provisioning Service Access Policy Name
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// The Provisioning Service Shared Access Key for the specified
        /// access policy
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// The Provisioning Service Shared Access Signature
        /// </summary>
        public string SharedAccessSignatureString { get; internal set; }

        /// <summary>
        /// The Repository to connect to
        /// </summary>
        public string RepositoryId { get; internal set; }

        /// <summary>
        /// The Provisioning Service Name
        /// </summary>
        public string ServiceName
        {
            get { return _serviceName; }
        }

        internal ServiceConnectionString ToServiceConnectionString()
        {
            Validate();
            return new ServiceConnectionString(this);
        }

        /// <summary>
        /// Returns the Provisioning Service Connection string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Validate();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, SharedAccessSignatureString);
            stringBuilder.AppendKeyValuePairIfNotEmpty(RepositoryIdPropertyName, RepositoryId);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private void Parse(string serviceConnectionString)
        {
            IDictionary<string, string> map = serviceConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignatureString = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            RepositoryId = GetConnectionStringOptionalValue(map, RepositoryIdPropertyName);
            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
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
            
            //TODO
            //ValidateFormatIfSpecified(RepositoryId, RepositoryIdPropertyName, SharedAccessSignatureRegex);
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
            _serviceName = GetServiceName(HostName);

            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name");
            }
        }

        private static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), nameof(value));
            }
        }

        private static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ValidateFormat(value, propertyName, regex);
            }
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            if (!map.TryGetValue(propertyName, out value))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The connection string is missing the property: {0}", propertyName),
                    nameof(map));
            }

            return value;
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            map.TryGetValue(propertyName, out value);
            return value;
        }

        private static string GetServiceName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string serviceName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return serviceName;
        }
    }
}
