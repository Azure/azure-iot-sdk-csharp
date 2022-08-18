// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Builds a connection string for the IoT hub service based on the properties populated by the user.
    /// </summary>
    public sealed class IotHubConnectionCredentials
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNamePropertyName = "HostName";
        private const string DeviceIdPropertyName = "DeviceId";
        private const string ModuleIdPropertyName = "ModuleId";
        private const string SharedAccessKeyNamePropertyName = "SharedAccessKeyName";
        private const string SharedAccessKeyPropertyName = "SharedAccessKey";
        private const string SharedAccessSignaturePropertyName = "SharedAccessSignature";
        private const string GatewayHostNamePropertyName = "GatewayHostName";

        private IAuthenticationMethod _authenticationMethod;

        /// <summary>
        /// Creates an instnace of this class based on an authentication method and the hostname of the IoT hub.
        /// </summary>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="gatewayHostName">The fully-qualified DNS host name of the gateway (optional).</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionCredentials"/> class with a populated connection string.</returns>
        public IotHubConnectionCredentials(IAuthenticationMethod authenticationMethod, string hostName, string gatewayHostName = null)
        {
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));

            AuthenticationMethod = authenticationMethod;
            HostName = hostName;
            GatewayHostName = gatewayHostName;

            if (authenticationMethod is DeviceAuthenticationWithX509Certificate)
            {
                UsingX509Cert = true;
            }

            Validate();
        }

        /// <summary>
        /// Creates an instance of this class using a connection string.
        /// </summary>
        /// <param name="iotHubConnectionString">The IoT hub device connection string.</param>
        /// <returns>A new instance of this class.</returns>
        public IotHubConnectionCredentials(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            // We'll parse the connection string and use that to build an auth method
            ExtractPropertiesFromConnectionString(iotHubConnectionString);
            AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethod(this);

            Validate();
        }

        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string HostName { get; internal set; }

        /// <summary>
        /// Gets the optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; internal set; }

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
        /// Gets or sets the authentication method to be used with the IoT hub service.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod
        {
            get => _authenticationMethod;
            set => SetAuthenticationMethod(value);
        }

        /// <summary>
        /// Indicates if the client uses X509 certificates for authenticating with IoT hub.
        /// </summary>
        public bool UsingX509Cert { get; internal set; }

        /// <summary>
        /// The client X509 certificates used for authenticating with IoT hub.
        /// </summary>
        // Device certificate
        internal X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The full chain of certificates from the one used to sign the client certificate to the one uploaded to the service.
        /// </summary>
        internal X509Certificate2Collection ChainCertificates { get; set; }

        // The suggested time to live value for tokens generated for SAS authenticated clients.
        internal TimeSpan SasTokenTimeToLive { get; set; }

        // The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        // This setting is valid only for SAS authenticated clients.
        internal int SasTokenRenewalBuffer { get; set; }

        /// <summary>
        /// Produces the connection string based on the values of the <see cref="IotHubConnectionCredentials"/> instance properties.
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
            stringBuilder.AppendKeyValuePairIfNotEmpty(GatewayHostNamePropertyName, GatewayHostName);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        private void ExtractPropertiesFromConnectionString(string iotHubConnectionString)
        {
            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);
            DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
        }

        internal void Validate()
        {
            // Host name
            if (HostName.IsNullOrWhiteSpace())
            {
                throw new FormatException("IoT hub hostname must be specified.");
            }

            // Device Id
            if (DeviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("DeviceId must be specified.");
            }

            // Shared access key
            if (!SharedAccessKey.IsNullOrWhiteSpace())
            {
                // Check that the shared access key supplied is a base64 string
                Convert.FromBase64String(SharedAccessKey);
            }

            // Shared access signature
            if (!string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                // Parse the supplied shared access signature string
                // and throw exception if the string is not in the expected format.
                _ = SharedAccessSignatureParser.Parse(SharedAccessSignature);
            }

            if (!(SharedAccessKey.IsNullOrWhiteSpace() ^ SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                if (!(UsingX509Cert || AuthenticationMethod is AuthenticationWithTokenRefresh))
                {
                    throw new ArgumentException(
                        "Should specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is not used");
                }
            }

            if ((UsingX509Cert || Certificate != null)
                && (!SharedAccessKey.IsNullOrWhiteSpace()
                    || !SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException(
                    "Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used");
            }
        }

        private void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            Argument.AssertNotNull(authMethod, nameof(authMethod));

            authMethod.Populate(this);
            _authenticationMethod = authMethod;
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
    }
}
