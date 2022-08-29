﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.Common.Security.SharedAccessSignature;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Builds a connection string for the IoT hub service based on the properties populated by the user.
    /// </summary>
    public class IotHubConnectionStringBuilder
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNameSeparator = ".";

        private const string HostNamePropertyName = "HostName";
        private const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        private const string SharedAccessKeyPropertyName = "SharedAccessKey";
        private const string SharedAccessSignaturePropertyName = "SharedAccessSignature";
        private const string DeviceIdPropertyName = "DeviceId";
        private const string ModuleIdPropertyName = "ModuleId";
        private const string GatewayHostNamePropertyName = "GatewayHostName";

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_hostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_idRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);

        private string _hostName;
        private IAuthenticationMethod _authenticationMethod;

        internal IotHubConnectionStringBuilder()
        {
        }

        /// <summary>
        /// Creates an <see cref="IotHubConnectionStringBuilder"/> object.
        /// </summary>
        /// <param name="hostname">The host name.</param>
        /// <param name="authenticationMethod">The authentication method.</param>
        /// <returns></returns>
        public static IotHubConnectionStringBuilder Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder
            {
                HostName = hostname,
                AuthenticationMethod = authenticationMethod
            };

            iotHubConnectionStringBuilder.Validate();

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Creates an <see cref="IotHubConnectionStringBuilder"/> object.
        /// </summary>
        /// <param name="iotHubConnectionString">The connection string.</param>
        public static IotHubConnectionStringBuilder Create(string iotHubConnectionString)
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder();
            iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
            iotHubConnectionStringBuilder.AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethod(iotHubConnectionStringBuilder);

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Gets or sets the host name.
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set => SetHostName(value);
        }

        /// <summary>
        /// Gets or sets the authentication method.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod
        {
            get => _authenticationMethod;
            set => SetAuthenticationMethod(value);
        }

        /// <summary>
        /// Gets the shared access key name.
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// Gets the shared access key value.
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// Gets the shared access key signature.
        /// </summary>
        public string SharedAccessSignature { get; internal set; }

        /// <summary>
        /// The Id of the device.
        /// </summary>
        public string DeviceId { get; internal set; }

        /// <summary>
        /// The Id of the module, if present.
        /// </summary>
        public string ModuleId { get; internal set; }

        /// <summary>
        /// The host name of the gateway, if present.
        /// </summary>
        public string GatewayHostName { get; internal set; }

        /// <summary>
        /// Gets the IoT hub name.
        /// </summary>
        public string IotHubName { get; private set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        public override string ToString()
        {
            Validate();

            var stringBuilder = new StringBuilder();
            AppendKeyValuePairIfNotEmpty(stringBuilder, HostNamePropertyName, HostName);
            if (SharedAccessKeyName != null)
            {
                AppendKeyValuePairIfNotEmpty(stringBuilder, SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            }
            else
            {
                if (ModuleId != null)
                {
                    AppendKeyValuePairIfNotEmpty(stringBuilder, ModuleIdPropertyName, ModuleId);
                }

                AppendKeyValuePairIfNotEmpty(stringBuilder, DeviceIdPropertyName, DeviceId);
                AppendKeyValuePairIfNotEmpty(stringBuilder, GatewayHostNamePropertyName, GatewayHostName);
            }

            AppendKeyValuePairIfNotEmpty(stringBuilder, SharedAccessKeyPropertyName, SharedAccessKey);
            AppendKeyValuePairIfNotEmpty(stringBuilder, SharedAccessSignaturePropertyName, SharedAccessSignature);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        internal void Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = ToDictionary(iotHubConnectionString, ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            Validate();
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(SharedAccessKeyName) && string.IsNullOrWhiteSpace(DeviceId))
            {
                throw new ArgumentException("Should specify either SharedAccessKeyName or DeviceId");
            }

            if (!(string.IsNullOrWhiteSpace(SharedAccessKey) ^ string.IsNullOrWhiteSpace(SharedAccessSignature)))
            {
                throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature");
            }

            if (string.IsNullOrWhiteSpace(IotHubName))
            {
                throw new FormatException("Missing IoT hub name");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
                Convert.FromBase64String(SharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(SharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(IotHubName, SharedAccessSignature);
            }

            ValidateFormat(HostName, HostNamePropertyName, s_hostNameRegex);
            if (!string.IsNullOrWhiteSpace(SharedAccessKeyName))
            {
                ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, s_sharedAccessKeyNameRegex);
            }
            if (!string.IsNullOrWhiteSpace(DeviceId))
            {
                ValidateFormatIfSpecified(DeviceId, DeviceIdPropertyName, s_idRegex);
            }
            if (!string.IsNullOrWhiteSpace(ModuleId))
            {
                ValidateFormatIfSpecified(ModuleId, ModuleIdPropertyName, s_idRegex);
            }
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, s_sharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignature, SharedAccessSignaturePropertyName, s_sharedAccessSignatureRegex);
            ValidateFormatIfSpecified(GatewayHostName, GatewayHostNamePropertyName, s_hostNameRegex);
        }

        internal void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, HostNamePropertyName, s_hostNameRegex);
            _hostName = hostname;
            SetIotHubName();
        }

        internal void SetIotHubName()
        {
            IotHubName = GetIotHubName(HostName);

            if (string.IsNullOrWhiteSpace(IotHubName))
            {
                throw new FormatException("Missing IoT hub name");
            }
        }

        internal void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            if (authMethod == null)
            {
                throw new ArgumentNullException(nameof(authMethod));
            }

            authMethod.Populate(this);
            _authenticationMethod = authMethod;
            Validate();
        }

        internal static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(
                    $"The connection string has an invalid value for property: {propertyName}",
                    nameof(value));
            }
        }

        internal static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ValidateFormat(value, propertyName, regex);
            }
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

        internal static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0
                ? hostName.Substring(0, index)
                : hostName;
            return iotHubName;
        }

        /// <summary>
        /// Takes a string representation of key/value pairs and produces a dictionary
        /// </summary>
        /// <param name="valuePairString">The string containing key/value pairs</param>
        /// <param name="kvpDelimiter">The delimeter between key/value pairs</param>
        /// <param name="kvpSeparator">The character separating each key and value</param>
        /// <returns>A dictionary of the key/value pairs</returns>
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

        /// <summary>
        /// Append a key value pair to a non-null <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The StringBuilder to append the key value pair to.</param>
        /// <param name="name">The key to be appended to the StringBuilder.</param>
        /// <param name="value">The value to be appended to the StringBuilder.</param>
        private static void AppendKeyValuePairIfNotEmpty(StringBuilder builder, string name, object value)
        {
            const char valuePairDelimiter = ';';
            const char valuePairSeparator = '=';

            if (value != null)
            {
                builder.Append(name);
                builder.Append(valuePairSeparator);
                builder.Append(value);
                builder.Append(valuePairDelimiter);
            }
        }
    }
}
