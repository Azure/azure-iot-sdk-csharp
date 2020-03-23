// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.Client.SharedAccessSignature;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Builds a connection string for the IoT Hub service based on the properties populated by the user.
    /// </summary>
    public sealed class IotHubConnectionStringBuilder
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNameSeparator = ".";
        private const string HostNamePropertyName = "HostName";
        private const string DeviceIdPropertyName = "DeviceId";
        private const string ModuleIdPropertyName = "ModuleId";
        private const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        private const string SharedAccessKeyPropertyName = "SharedAccessKey";
        private const string SharedAccessSignaturePropertyName = "SharedAccessSignature";
        private const string GatewayHostNamePropertyName = "GatewayHostName";
        private const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        private static readonly TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private const string X509CertPropertyName = "X509Cert";
        private static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex IdNameRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex X509CertRegex = new Regex(@"^[true|false]+$", regexOptions, regexTimeoutMilliseconds);

        private string _hostName;
        private IAuthenticationMethod _authenticationMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubConnectionStringBuilder"/> class.
        /// </summary>
        private IotHubConnectionStringBuilder()
        {
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT Hub and the authentication method passed as a parameter.
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(hostname, null, authenticationMethod);
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT Hub, the hostname of Gateway and the authentication method passed as a parameter.
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT Hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of the gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder()
            {
                HostName = hostname,
                GatewayHostName = gatewayHostname,
                AuthenticationMethod = authenticationMethod
            };

            iotHubConnectionStringBuilder.Validate();

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT Hub and the authentication method passed as a parameter.
        /// </summary>
        /// <param name="iotHubConnectionString">The connection string.</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string iotHubConnectionString)
        {
            if (iotHubConnectionString.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return CreateWithIAuthenticationOverride(iotHubConnectionString, null);
        }

        internal static IotHubConnectionStringBuilder CreateWithIAuthenticationOverride(
            string iotHubConnectionString,
            IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder()
            {
                HostName = "TEMP.HUB",
            };

            if (authenticationMethod == null)
            {
                iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
                iotHubConnectionStringBuilder.AuthenticationMethod =
                    AuthenticationMethodFactory.GetAuthenticationMethod(iotHubConnectionStringBuilder);
            }
            else
            {
                iotHubConnectionStringBuilder.AuthenticationMethod = authenticationMethod;
                iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
            }

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the IoT Hub service.
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set => SetHostName(value);
        }

        /// <summary>
        /// Gets or sets the authentication method to be used with the IoT Hub service.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod
        {
            get => _authenticationMethod;
            set => SetAuthenticationMethod(value);
        }

        /// <summary>
        /// Gets the device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; internal set; }

        /// <summary>
        /// Gets the module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; internal set; }

        /// <summary>
        /// Gets the shared acess key name used to connect the device to the IoT Hub service.
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// Gets the shared access key used to connect to the IoT Hub service.
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// Gets the optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; internal set; }

        /// <summary>
        /// Gets the shared access signature used to connect to the IoT Hub service.
        /// </summary>
        public string SharedAccessSignature { get; internal set; }

        public bool UsingX509Cert { get; internal set; }

        internal string IotHubName { get; private set; }

        internal X509Certificate2 Certificate { get; set; }

        internal IotHubConnectionString ToIotHubConnectionString()
        {
            Validate();
            return new IotHubConnectionString(this);
        }

        /// <summary>
        /// Produces the connection string based on the values of the <see cref="IotHubConnectionStringBuilder"/> instance properties.
        /// </summary>
        /// <returns>A properly formatted connection string.</returns>
        public override sealed string ToString()
        {
            Validate();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(DeviceIdPropertyName, DeviceId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(ModuleIdPropertyName, ModuleId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, SharedAccessSignature);
            stringBuilder.AppendKeyValuePairIfNotEmpty(X509CertPropertyName, UsingX509Cert);
            stringBuilder.AppendKeyValuePairIfNotEmpty(GatewayHostNamePropertyName, GatewayHostName);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private void Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            UsingX509Cert = GetConnectionStringOptionalValueOrDefault<bool>(map, X509CertPropertyName, GetX509, true);
            GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            Validate();
        }

        private void Validate()
        {
            if (DeviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("DeviceId must be specified in connection string");
            }

            if (!(SharedAccessKey.IsNullOrWhiteSpace() ^ SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                if (!(UsingX509Cert || (AuthenticationMethod is AuthenticationWithTokenRefresh)))
                {
                    throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is not used");
                }
            }

            if ((UsingX509Cert || Certificate != null) &&
                (!SharedAccessKey.IsNullOrWhiteSpace() || !SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException("Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used");
            }

            if (IotHubName.IsNullOrWhiteSpace())
            {
                throw new FormatException("Missing IoT hub name");
            }

            if (!SharedAccessKey.IsNullOrWhiteSpace())
            {
                Convert.FromBase64String(SharedAccessKey);
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                if (SharedAccessSignatureParser.IsSharedAccessSignature(SharedAccessSignature))
                {
                    SharedAccessSignatureParser.Parse(IotHubName, SharedAccessSignature);
                }
                else
                {
                    throw new ArgumentException("Invalid SharedAccessSignature (SAS)");
                }
            }

            ValidateFormat(HostName, HostNamePropertyName, HostNameRegex);
            ValidateFormat(DeviceId, DeviceIdPropertyName, IdNameRegex);
            if (!string.IsNullOrEmpty(ModuleId))
            {
                ValidateFormat(ModuleId, DeviceIdPropertyName, IdNameRegex);
            }

            ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignature, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(GatewayHostName, GatewayHostNamePropertyName, HostNameRegex);
            ValidateFormatIfSpecified(UsingX509Cert.ToString(CultureInfo.InvariantCulture), X509CertPropertyName, X509CertRegex);
        }

        private void SetHostName(string hostname)
        {
            if (hostname.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, HostNamePropertyName, HostNameRegex);

            _hostName = hostname;
            SetIotHubName();
        }

        private void SetIotHubName()
        {
            IotHubName = GetIotHubName(HostName);

            if (IotHubName.IsNullOrWhiteSpace())
            {
                throw new FormatException("Missing IOT hub name");
            }
        }

        private void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            if (authMethod == null)
            {
                throw new ArgumentNullException(nameof(authMethod));
            }

            authMethod.Populate(this);
            _authenticationMethod = authMethod;
            Validate();
        }

        private static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException($"The connection string has an invalid value for property: {propertyName}.");
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
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new ArgumentException($"The connection string is missing the property: {propertyName}.");
            }

            return value;
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            map.TryGetValue(propertyName, out string value);
            return value;
        }

        private static TValue GetConnectionStringOptionalValueOrDefault<TValue>(
            IDictionary<string, string> map,
            string propertyName,
            TryParse<string, TValue> tryParse,
            bool ignoreCase)
        {
            var value = default(TValue);
            if (map.TryGetValue(propertyName, out string stringValue) && stringValue != null)
            {
                if (!tryParse(stringValue, ignoreCase, out value))
                {
                    throw new ArgumentException($"The connection string has an invalid value for property: {propertyName}");
                }
            }

            return value;
        }

        private static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0 ? hostName.Substring(0, index) : null;
            return iotHubName;
        }

        private static bool GetX509(string input, bool ignoreCase, out bool usingX509Cert)
        {
            usingX509Cert = false;

            bool isMatch = string.Equals(input, "true", ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (isMatch)
            {
                usingX509Cert = true;
            }

            return true;
        }
    }
}
