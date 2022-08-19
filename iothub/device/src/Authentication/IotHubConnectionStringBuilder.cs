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
    public sealed class IotHubConnectionStringBuilder
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

        /// <summary>
        /// Creates an instnace of this class based on an authentication method and the hostname of the IoT hub.
        /// </summary>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="gatewayHostName">The fully-qualified DNS host name of the gateway (optional).</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionStringBuilder"/> class with a populated connection string.</returns>
        public IotHubConnectionStringBuilder(IAuthenticationMethod authenticationMethod, string hostName, string gatewayHostName = null)
        {
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));

            HostName = hostName;
            GatewayHostName = gatewayHostName;

            AuthenticationMethod = authenticationMethod;
            AuthenticationMethod.Populate(this);

            Validate();
        }

        /// <summary>
        /// Creates an instance of this class using a connection string.
        /// </summary>
        /// <param name="iotHubConnectionString">The IoT hub device connection string.</param>
        /// <returns>A new instance of this class.</returns>
        public IotHubConnectionStringBuilder(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            // We'll parse the connection string and use that to build an auth method
            ExtractPropertiesFromConnectionString(iotHubConnectionString);
            AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethodFromConnectionString(this);

            Validate();
        }

        /// <summary>
        /// Gets or sets the value of the fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// Gets the optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; private set; }

        /// <summary>
        /// Gets the device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets the module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// Gets the shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        public string SharedAccessKeyName { get; set; }

        /// <summary>
        /// Gets the shared access key used to connect to the IoT hub service.
        /// </summary>
        public string SharedAccessKey { get; set; }

        /// <summary>
        /// Gets the shared access signature used to connect to the IoT hub service.
        /// </summary>
        /// <remarks>
        /// This is used when a device app creates its own limited-lifespan SAS token, instead of letting
        /// this SDK derive one from a shared access token. When a device client is initialized with a
        /// SAS token, when that token expires, the client must be disposed, and if desired, recreated
        /// with a newly derived SAS token.
        /// </remarks>
        public string SharedAccessSignature { get; set; }

        /// <summary>
        /// The client X509 certificates used for authenticating with IoT hub.
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// The full chain of certificates from the one used to sign the client certificate to the one uploaded to the service.
        /// </summary>
        public X509Certificate2Collection ChainCertificates { get; set; }

        /// <summary>
        /// Gets or sets the authentication method to be used with the IoT hub service.
        /// </summary>
        internal IAuthenticationMethod AuthenticationMethod { get; }

        // The suggested time to live value for tokens generated for SAS authenticated clients.
        internal TimeSpan SasTokenTimeToLive { get; set; }

        // The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        // This setting is valid only for SAS authenticated clients.
        internal int SasTokenRenewalBuffer { get; set; }

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
            if (!SharedAccessSignature.IsNullOrWhiteSpace())
            {
                // Parse the supplied shared access signature string
                // and throw exception if the string is not in the expected format.
                _ = SharedAccessSignatureParser.Parse(SharedAccessSignature);
            }

            // Either shared access key, shared access signature or X.509 certificate is required for authenticating the client with IoT hub.
            if (Certificate == null
                && SharedAccessKey.IsNullOrWhiteSpace()
                    && SharedAccessSignature.IsNullOrWhiteSpace())
            {
                throw new ArgumentException(
                        "Should specify either SharedAccessKey, SharedAccessSignature or X.509 certificate for authenticating the client with IoT hub.");
            }

            // If an X.509 certificate is supplied then neither shared access key nor shared access signature should be supplied.
            if (Certificate != null
                && (!SharedAccessKey.IsNullOrWhiteSpace()
                    || !SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException(
                    "Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used for authenticating the client with IoT hub.");
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
    }
}
