// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.Client.SharedAccessSignature;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Builds a connection string for the IoT hub service based on the properties populated by the user.
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

        // For some reason, the .NET SDK originally expected "X509Cert=true" in a connection string to inform the SDK that it would not
        // include a shared access key, and to not error when parsing a connection string. However, the other SDK languages all followed
        // the same key/value pair of "x509=true".
        // So now we're adding support for it to work either way, so we stay compliant with the past but can improve documentation so all
        // SDK languages refer to the same key/value pair naming.
        private const string X509CertPropertyName = "X509Cert";

        private const string CommonX509CertPropertyName = "x509";

        private const RegexOptions CommonRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_hostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", CommonRegexOptions, s_regexTimeoutMilliseconds);
        private static readonly Regex s_idNameRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", CommonRegexOptions, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", CommonRegexOptions, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyRegex = new Regex(@"^.+$", CommonRegexOptions, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessSignatureRegex = new Regex(@"^.+$", CommonRegexOptions, s_regexTimeoutMilliseconds);
        private static readonly Regex s_x509CertRegex = new Regex(@"^[true|false]+$", CommonRegexOptions, s_regexTimeoutMilliseconds);

        private string _hostName;
        private IAuthenticationMethod _authenticationMethod;

        /// <summary>
        /// Initializes a new instance of the this class.
        /// </summary>
        internal IotHubConnectionStringBuilder()
        {
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT hub and the authentication method passed as a parameter.
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            return Create(hostname, null, authenticationMethod);
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT hub, the hostname of Gateway and the authentication
        /// method passed as a parameter.
        /// </summary>
        /// <param name="hostname">The fully-qualified DNS hostname of IoT hub</param>
        /// <param name="gatewayHostname">The fully-qualified DNS hostname of the gateway</param>
        /// <param name="authenticationMethod">The authentication method that is used</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string hostname, string gatewayHostname, IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder
            {
                HostName = hostname,
                GatewayHostName = gatewayHostname,
                AuthenticationMethod = authenticationMethod
            };

            iotHubConnectionStringBuilder.Validate();

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Creates a connection string based on the hostname of the IoT hub and the authentication method passed as a parameter.
        /// </summary>
        /// <param name="iotHubConnectionString">The connection string.</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public static IotHubConnectionStringBuilder Create(string iotHubConnectionString)
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException(nameof(iotHubConnectionString));
            }

            return CreateWithIAuthenticationOverride(iotHubConnectionString, null);
        }

        internal static IotHubConnectionStringBuilder CreateWithIAuthenticationOverride(
            string iotHubConnectionString,
            IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder
            {
                HostName = "TEMP.HUB",
            };

            if (authenticationMethod == null)
            {
                iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
                iotHubConnectionStringBuilder.AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethod(
                    iotHubConnectionStringBuilder);
            }
            else
            {
                iotHubConnectionStringBuilder.AuthenticationMethod = authenticationMethod;
                iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
            }

            return iotHubConnectionStringBuilder;
        }

        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set => SetHostName(value);
        }

        /// <summary>
        /// Gets or sets the authentication method to be used with the IoT hub service.
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
        /// Gets the shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        public string SharedAccessKeyName { get; internal set; }

        /// <summary>
        /// Gets the shared access key used to connect to the IoT hub service.
        /// </summary>
        public string SharedAccessKey { get; internal set; }

        /// <summary>
        /// Gets the optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; internal set; }

        /// <summary>
        /// Gets the shared access signature used to connect to the IoT hub service.
        /// </summary>
        /// <remarks>
        /// This is used when a device app creates its own limited-lifespan SAS token, instead of letting
        /// this SDK derive one from a shared access token. When a device client is initialized with a
        /// SAS token, when that token expires, the client must be disposed, and if desired, recreated
        /// with a newly derived SAS token.
        /// </remarks>
        public string SharedAccessSignature { get; internal set; }

        /// <summary>
        /// Indicates if the connection string indicates if an x509 certificate is specified for authentication.
        /// </summary>
        public bool UsingX509Cert { get; internal set; }

        internal string IotHubName { get; private set; }

        // Device certificate
        internal X509Certificate2 Certificate { get; set; }

        // Full chain of certificates from the one used to sign the device certificate to the one uploaded to the service.
        internal X509Certificate2Collection ChainCertificates { get; set; }

        // The suggested time to live value for tokens generated for SAS authenticated clients.
        internal TimeSpan SasTokenTimeToLive { get; set; }

        // The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        // This setting is valid only for SAS authenticated clients.
        internal int SasTokenRenewalBuffer { get; set; }

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
            stringBuilder.AppendKeyValuePairIfNotEmpty(CommonX509CertPropertyName, UsingX509Cert);
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
            UsingX509Cert = GetConnectionStringOptionalValueOrDefault<bool>(map, X509CertPropertyName, ParseX509, true)
                || GetConnectionStringOptionalValueOrDefault<bool>(map, CommonX509CertPropertyName, ParseX509, true);
            GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                throw new ArgumentException("DeviceId must be specified in connection string");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey) ^ string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                if (!(UsingX509Cert || AuthenticationMethod is AuthenticationWithTokenRefresh))
                {
                    throw new ArgumentException(
                        "Should specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is not used");
                }
            }

            if ((UsingX509Cert || Certificate != null) &&
                (!string.IsNullOrWhiteSpace(SharedAccessKey)
                    || !string.IsNullOrWhiteSpace(SharedAccessSignature)))
            {
                throw new ArgumentException(
                    "Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used");
            }

            if (string.IsNullOrWhiteSpace(IotHubName))
            {
                throw new FormatException("Missing IoT hub name");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
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

            ValidateFormat(HostName, HostNamePropertyName, s_hostNameRegex);
            ValidateFormat(DeviceId, DeviceIdPropertyName, s_idNameRegex);
            if (!string.IsNullOrEmpty(ModuleId))
            {
                ValidateFormat(ModuleId, ModuleIdPropertyName, s_idNameRegex);
            }

            ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, s_sharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, s_sharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignature, SharedAccessSignaturePropertyName, s_sharedAccessSignatureRegex);
            ValidateFormatIfSpecified(GatewayHostName, GatewayHostNamePropertyName, s_hostNameRegex);
            ValidateFormatIfSpecified(UsingX509Cert.ToString(CultureInfo.InvariantCulture), X509CertPropertyName, s_x509CertRegex);
        }

        private void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            ValidateFormat(hostname, HostNamePropertyName, s_hostNameRegex);

            _hostName = hostname;
            SetIotHubName();
        }

        private void SetIotHubName()
        {
            IotHubName = GetIotHubName(HostName);

            // We expect the hostname to be of the format "acme.azure-devices.net", in which case the IotHubName is "acme".
            // For transparent gateway scenarios, we can simplify the input credentials to only specify the gateway device hostname,
            // instead of having to specify both the IoT hub hostname and the gateway device hostname.
            // In this case, the hostname will be of the format "myGatewayDevice", and will not have ".azure-devices.net" suffix.
            if (string.IsNullOrWhiteSpace(IotHubName))
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, $"Connecting to a gateway device with hostname=[{HostName}]");
                IotHubName = HostName;
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
            if (map.TryGetValue(propertyName, out string stringValue)
                && stringValue != null)
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

        private static bool ParseX509(string input, bool ignoreCase, out bool isUsingX509Cert)
        {
            isUsingX509Cert = false;

            bool isMatch = string.Equals(
                input,
                "true",
                ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal);
            if (isMatch)
            {
                isUsingX509Cert = true;
            }

            // Always returns true, but must return a bool because it is used in a delegate that requires that return.
            return true;
        }
    }
}
