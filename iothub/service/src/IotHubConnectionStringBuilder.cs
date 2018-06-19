// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Devices.Common;
    using SharedAccessSignatureParser = Microsoft.Azure.Devices.Common.Security.SharedAccessSignature;

    /// <summary>
    /// Builds a connection string for the IoT Hub service based on the properties populated by the user.
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

        private static readonly TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);
        private static readonly Regex IdRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);

        private string hostName;
        private IAuthenticationMethod authenticationMethod;

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
            get { return this.hostName; }
            set { this.SetHostName(value); }
        }

        /// <summary>
        /// Gets or sets the authentication method.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod
        {
            get { return this.authenticationMethod; }
            set { this.SetAuthenticationMethod(value); }
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

        public string DeviceId { get; internal set; }

        public string ModuleId { get; internal set; }

        public string GatewayHostName { get; internal set; }

        /// <summary>
        /// Gets the IoT Hub name.
        /// </summary>
        public string IotHubName
        {
            get { return this.iotHubName; }
        }

        internal IotHubConnectionString ToIotHubConnectionString()
        {
            this.Validate();
            return new IotHubConnectionString(this);
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        public override string ToString()
        {
            this.Validate();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, this.HostName);
            if (this.SharedAccessKeyName != null)
            {
                stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, this.SharedAccessKeyName);
            }            
            else
            {
                if (this.ModuleId != null)
                {
                    stringBuilder.AppendKeyValuePairIfNotEmpty(ModuleIdPropertyName, this.ModuleId);
                }

                stringBuilder.AppendKeyValuePairIfNotEmpty(DeviceIdPropertyName, this.DeviceId);
                stringBuilder.AppendKeyValuePairIfNotEmpty(GatewayHostNamePropertyName, this.GatewayHostName);
            }

            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, this.SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, this.SharedAccessSignature);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        internal void Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            this.HostName = GetConnectionStringValue(map, HostNamePropertyName);
            this.SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            this.SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            this.SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            this.DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            this.ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            this.GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            this.Validate();
        }

        internal void Validate()
        {
            if (this.SharedAccessKeyName.IsNullOrWhiteSpace() && this.DeviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Should specify either SharedAccessKeyName or DeviceId");
            }

            if (!(this.SharedAccessKey.IsNullOrWhiteSpace() ^ this.SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature");
            }

            if (string.IsNullOrWhiteSpace(this.IotHubName))
            {
                throw new FormatException("Missing IOT hub name");
            }

            if (!this.SharedAccessKey.IsNullOrWhiteSpace())
            {
                Convert.FromBase64String(this.SharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(this.SharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(this.IotHubName, this.SharedAccessSignature);
            }

            ValidateFormat(this.HostName, HostNamePropertyName, HostNameRegex);
            if (!this.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            }
            if (!this.DeviceId.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.DeviceId, DeviceIdPropertyName, IdRegex);
            }
            if (!this.ModuleId.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.ModuleId, ModuleIdPropertyName, IdRegex);
            }
            ValidateFormatIfSpecified(this.SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(this.SharedAccessSignature, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(this.GatewayHostName, GatewayHostNamePropertyName, HostNameRegex);
        }

        internal void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, HostNamePropertyName, HostNameRegex);
            this.hostName = hostname;
            this.SetIotHubName();
        }

        internal void SetIotHubName()
        {
            this.IotHubName = GetIotHubName(this.HostName);

            if (string.IsNullOrWhiteSpace(this.IotHubName))
            {
                throw new FormatException("Missing IOT hub name");
            }
        }

        internal void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            if (authMethod == null)
            {
                throw new ArgumentNullException(nameof(authMethod));
            }

            authMethod.Populate(this);
            this.authenticationMethod = authMethod;
            this.Validate();
        }

        internal static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), nameof(value));
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
            string value;
            if (!map.TryGetValue(propertyName, out value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string is missing the property: {0}", propertyName), nameof(map));
            }

            return value;
        }

        internal static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            map.TryGetValue(propertyName, out value);
            return value;
        }

        internal static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return iotHubName;
        }
    }
}
