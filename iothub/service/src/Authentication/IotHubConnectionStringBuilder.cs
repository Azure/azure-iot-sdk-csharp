// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.SharedAccessSignature;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Builds a connection string for the IoT hub service based on the properties populated by the user.
    /// </summary>
    public class IotHubConnectionStringBuilder
    {
        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_hostNameRegex = new(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyNameRegex = new(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyRegex = new(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessSignatureRegex = new(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);

        private string _hostName;
        private IAuthenticationMethod _authenticationMethod;

        internal IotHubConnectionStringBuilder()
        {
        }

        internal IotHubConnectionStringBuilder(string hostName, string sharedAccessKeyName, string sharedAccessKey, string sharedAccessSignature, string gatewayHostName)
        {
            HostName = hostName;
            SharedAccessKeyName = sharedAccessKeyName;
            SharedAccessKey = sharedAccessKey;
            SharedAccessSignature = sharedAccessSignature;
            GatewayHostName = gatewayHostName;
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

            IotHubConnectionStringBuilder iotHubConnectionStringBuilder = IotHubConnectionStringParser.Parse(iotHubConnectionString);
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
            AppendKeyValuePairIfNotEmpty(stringBuilder, IotHubConnectionStringConstants.HostNamePropertyName, HostName);
            if (SharedAccessKeyName != null)
            {
                AppendKeyValuePairIfNotEmpty(stringBuilder, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            }
            else
            {
                AppendKeyValuePairIfNotEmpty(stringBuilder, IotHubConnectionStringConstants.GatewayHostNamePropertyName, GatewayHostName);
            }

            AppendKeyValuePairIfNotEmpty(stringBuilder, IotHubConnectionStringConstants.SharedAccessKeyPropertyName, SharedAccessKey);
            AppendKeyValuePairIfNotEmpty(stringBuilder, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName, SharedAccessSignature);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(SharedAccessKeyName))
            {
                throw new ArgumentException("Should specify either SharedAccessKeyName.");
            }

            if (!(string.IsNullOrWhiteSpace(SharedAccessKey) ^ string.IsNullOrWhiteSpace(SharedAccessSignature)))
            {
                throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature.");
            }

            if (string.IsNullOrWhiteSpace(IotHubName))
            {
                throw new FormatException("Missing IoT hub name.");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
                Convert.FromBase64String(SharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(SharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(IotHubName, SharedAccessSignature);
            }

            ValidateFormat(HostName, IotHubConnectionStringConstants.HostNamePropertyName, s_hostNameRegex);
            if (!string.IsNullOrWhiteSpace(SharedAccessKeyName))
            {
                ValidateFormatIfSpecified(SharedAccessKeyName, IotHubConnectionStringConstants.SharedAccessKeyNamePropertyName, s_sharedAccessKeyNameRegex);
            }
            ValidateFormatIfSpecified(SharedAccessKey, IotHubConnectionStringConstants.SharedAccessKeyPropertyName, s_sharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignature, IotHubConnectionStringConstants.SharedAccessSignaturePropertyName, s_sharedAccessSignatureRegex);
            ValidateFormatIfSpecified(GatewayHostName, IotHubConnectionStringConstants.GatewayHostNamePropertyName, s_hostNameRegex);
        }

        internal void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, IotHubConnectionStringConstants.HostNamePropertyName, s_hostNameRegex);
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

        internal static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(IotHubConnectionStringConstants.HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0
                ? hostName.Substring(0, index)
                : hostName;
            return iotHubName;
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
