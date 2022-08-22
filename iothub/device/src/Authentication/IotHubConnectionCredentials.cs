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
    /// Holder for client credentials that will be used for authenticating the client with IoT hub service.
    /// </summary>
    public sealed class IotHubConnectionCredentials
    {
        /// <summary>
        /// Creates an instnace of this class based on an authentication method, the host name of the IoT hub and an optional gateway host name.
        /// </summary>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="gatewayHostName">The fully-qualified DNS host name of the gateway (optional).</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionCredentials"/> class with a populated connection string.</returns>
        public IotHubConnectionCredentials(IAuthenticationMethod authenticationMethod, string hostName, string gatewayHostName = null)
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
        public IotHubConnectionCredentials(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            // We'll parse the connection string and use that to build an auth method
            IotHubConnectionString parsedConnectionString = IotHubConnectionStringParser.Parse(iotHubConnectionString);
            AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethodFromConnectionString(parsedConnectionString);

            PopulatePropertiesFromConnectionString(parsedConnectionString);

            Validate();
        }

        /// <summary>
        /// The fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// The optional name of the gateway to connect to
        /// </summary>
        public string GatewayHostName { get; private set; }

        /// <summary>
        /// The device identifier of the device connecting to the service.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// The module identifier of the module connecting to the service.
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// The shared access key name used to connect the device to the IoT hub service.
        /// </summary>
        public string SharedAccessKeyName { get; set; }

        /// <summary>
        /// The shared access key used to connect to the IoT hub service.
        /// </summary>
        public string SharedAccessKey { get; set; }

        /// <summary>
        /// The shared access signature used to connect to the IoT hub service.
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
        /// The suggested time to live value for tokens generated for SAS authenticated clients.
        /// </summary>
        internal TimeSpan SasTokenTimeToLive { get; set; }

        /// <summary>
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// </summary>
        internal int SasTokenRenewalBuffer { get; set; }

        /// <summary>
        /// The authentication method to be used with the IoT hub service.
        /// </summary>
        internal IAuthenticationMethod AuthenticationMethod { get; }

        /// <summary>
        /// The token refresh logic to be used for clients authenticating with either an AuthenticationWithTokenRefresh IAuthenticationMethod mechanism
        /// or throw a shared access key value that can be used by the SDK to generate SAS tokens.
        /// </summary>
        internal AuthenticationWithTokenRefresh SasTokenRefresher { get; }

        private void PopulatePropertiesFromConnectionString(IotHubConnectionString iotHubConnectionString)
        {
            HostName = iotHubConnectionString.HostName;
            GatewayHostName = iotHubConnectionString.GatewayHostName;
            DeviceId = iotHubConnectionString.DeviceId;
            ModuleId = iotHubConnectionString.ModuleId;
            SharedAccessKeyName = iotHubConnectionString.SharedAccessKeyName;
            SharedAccessKey = iotHubConnectionString.SharedAccessKey;
            SharedAccessSignature = iotHubConnectionString.SharedAccessSignature;
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
            // These values should be populated in the constructor. The only exception to this scenario is when the authentication method is
            // AuthenticationWithTokenRefresh, in which case the shared access signature is initially null and is generated on demand during client authentication.
            if (Certificate == null
                && SharedAccessKey.IsNullOrWhiteSpace()
                && SharedAccessSignature.IsNullOrWhiteSpace()
                && AuthenticationMethod is not AuthenticationWithTokenRefresh)
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
    }
}
