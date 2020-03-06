// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;

#if !NETMF

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.Client.SharedAccessSignature;
using System.Security.Cryptography.X509Certificates;

#endif

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

#if !NETMF
        private const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        private static readonly TimeSpan regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private const string X509CertPropertyName = "X509Cert";
        private static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex IdNameRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", regexOptions, regexTimeoutMilliseconds);
        private static readonly Regex X509CertRegex = new Regex(@"^[true|false]+$", regexOptions, regexTimeoutMilliseconds);
#endif

        private string hostName;
        private IAuthenticationMethod authenticationMethod;

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
                throw new ArgumentNullException("iotHubConnectionString");
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
            get { return this.hostName; }
            set { this.SetHostName(value); }
        }

        /// <summary>
        /// Gets or sets the authentication method to be used with the IoT Hub service.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod
        {
            get { return this.authenticationMethod; }
            set { this.SetAuthenticationMethod(value); }
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

#if !NETMF
        internal X509Certificate2 Certificate { get; set; }
#endif

        internal IotHubConnectionString ToIotHubConnectionString()
        {
            this.Validate();
            return new IotHubConnectionString(this);
        }

#if !NETMF

        /// <summary>
        /// Produces the connection string based on the values of the <see cref="IotHubConnectionStringBuilder"/> instance properties.
        /// </summary>
        /// <returns>A properly formatted connection string.</returns>
        public override sealed string ToString()
        {
            this.Validate();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, this.HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(DeviceIdPropertyName, this.DeviceId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(ModuleIdPropertyName, this.ModuleId);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, this.SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, this.SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, this.SharedAccessSignature);
            stringBuilder.AppendKeyValuePairIfNotEmpty(X509CertPropertyName, this.UsingX509Cert);
            stringBuilder.AppendKeyValuePairIfNotEmpty(GatewayHostNamePropertyName, this.GatewayHostName);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

#endif

        private void Parse(string iotHubConnectionString)
        {
#if NETMF
            if (iotHubConnectionString.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Malformed Token");
            }

            string[] parts = iotHubConnectionString.Split(ValuePairDelimiter);

            foreach (string part in parts)
            {
                string[] pair = part.Split(new char[] { ValuePairSeparator }, 2);

                if (pair.Length > 1)
                {
                    if (string.Equals(pair[0].ToLower(), HostNamePropertyName.ToLower()))
                    {
                        // Host Name
                        this.HostName = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), DeviceIdPropertyName.ToLower()))
                    {
                        // DeviceId
                        this.DeviceId = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), ModuleIdPropertyName.ToLower()))
                    {
                        // ModuleId
                        this.ModuleId = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), SharedAccessKeyNamePropertyName.ToLower()))
                    {
                        // Shared Access Key Name
                        this.SharedAccessKeyName = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), SharedAccessKeyPropertyName.ToLower()))
                    {
                        // Shared Access Key
                        // need to handle this differently because shared access key may have special chars such as '=' which break the string split
                        this.SharedAccessKey = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), SharedAccessSignaturePropertyName.ToLower()))
                    {
                        // Shared Access Signature
                        // need to handle this differently because shared access key may have special chars such as '=' which break the string split
                        this.SharedAccessSignature = pair[1];
                    }
                    else if (string.Equals(pair[0].ToLower(), GatewayHostNamePropertyName.ToLower()))
                    {
                        // Gateway host name
                        this.GatewayHostName = pair[1];
                    }
                }
            }
#else
            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            this.HostName = GetConnectionStringValue(map, HostNamePropertyName);
            this.DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            this.ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            this.SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            this.SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            this.SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            this.UsingX509Cert = GetConnectionStringOptionalValueOrDefault<bool>(map, X509CertPropertyName, GetX509, true);
            this.GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);
#endif

            this.Validate();
        }

        private void Validate()
        {
            if (this.DeviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("DeviceId must be specified in connection string");
            }

            if (!(this.SharedAccessKey.IsNullOrWhiteSpace() ^ this.SharedAccessSignature.IsNullOrWhiteSpace()))
            {
#if !NETMF
                if (!(this.UsingX509Cert || (this.AuthenticationMethod is AuthenticationWithTokenRefresh)))
                {
#endif
                    throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is not used");
#if !NETMF
                }
#endif
            }

#if !NETMF
            if ((this.UsingX509Cert || this.Certificate != null) &&
                (!this.SharedAccessKey.IsNullOrWhiteSpace() || !this.SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException("Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used");
            }
#endif

            if (this.IotHubName.IsNullOrWhiteSpace())
            {
#if NETMF
                throw new ArgumentException("Missing IOT hub name");
#else
                throw new FormatException("Missing IOT hub name");
#endif
            }

            if (!this.SharedAccessKey.IsNullOrWhiteSpace())
            {
                Convert.FromBase64String(this.SharedAccessKey);
            }

#if !NETMF
            if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
            {
                if (SharedAccessSignatureParser.IsSharedAccessSignature(this.SharedAccessSignature))
                {
                    SharedAccessSignatureParser.Parse(this.IotHubName, this.SharedAccessSignature);
                }
                else
                {
                    throw new ArgumentException("Invalid SharedAccessSignature (SAS)");
                }
            }

            ValidateFormat(this.HostName, HostNamePropertyName, HostNameRegex);
            ValidateFormat(this.DeviceId, DeviceIdPropertyName, IdNameRegex);
            if (!string.IsNullOrEmpty(this.ModuleId))
            {
                ValidateFormat(this.ModuleId, DeviceIdPropertyName, IdNameRegex);
            }

            ValidateFormatIfSpecified(this.SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(this.SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(this.SharedAccessSignature, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(this.GatewayHostName, GatewayHostNamePropertyName, HostNameRegex);
            ValidateFormatIfSpecified(this.UsingX509Cert.ToString(), X509CertPropertyName, X509CertRegex);
#endif
        }

        private void SetHostName(string hostname)
        {
            if (hostname.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("hostname");
            }
#if !NETMF
            ValidateFormat(hostname, HostNamePropertyName, HostNameRegex);
#endif

            this.hostName = hostname;
            this.SetIotHubName();
        }

        private void SetIotHubName()
        {
            this.IotHubName = GetIotHubName(this.HostName);

            if (this.IotHubName.IsNullOrWhiteSpace())
            {
#if NETMF
                throw new ArgumentException("Missing IOT hub name");
#else
                throw new FormatException("Missing IOT hub name");
#endif
            }
        }

        private void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            if (authMethod == null)
            {
                throw new ArgumentNullException("authMethod");
            }

            authMethod.Populate(this);
            this.authenticationMethod = authMethod;
            this.Validate();
        }

#if !NETMF

        private static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), "iotHubConnectionString");
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
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string is missing the property: {0}", propertyName), "iotHubConnectionString");
            }

            return value;
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            map.TryGetValue(propertyName, out value);
            return value;
        }

        private static TValue GetConnectionStringOptionalValueOrDefault<TValue>(IDictionary<string, string> map, string propertyName, TryParse<string, TValue> tryParse, bool ignoreCase)
        {
            TValue value = default(TValue);
            string stringValue;
            if (map.TryGetValue(propertyName, out stringValue) && stringValue != null)
            {
                if (!tryParse(stringValue, ignoreCase, out value))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), "iotHubConnectionString");
                }
            }

            return value;
        }

#endif

        private static string GetIotHubName(string hostName)
        {
#if NETMF
            int index = hostName.IndexOf(HostNameSeparator);
#else
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
#endif
            string iotHubName = index >= 0 ? hostName.Substring(0, index) : null;
            return iotHubName;
        }

        private static bool GetX509(string input, bool ignoreCase, out bool usingX509Cert)
        {
            usingX509Cert = false;
            bool isMatch;

#if NETMF
            isMatch = (ignoreCase ? input.ToLower().CompareTo("true") : input.CompareTo("true")) == 0;
#else
            isMatch = string.Equals(input, "true", ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
#endif

            if (isMatch)
            {
                usingX509Cert = true;
            }

            return true;
        }
    }
}
