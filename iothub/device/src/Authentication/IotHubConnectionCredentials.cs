// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Holder for client credentials that will be used for authenticating the client with IoT hub service.
    /// </summary>
    public sealed class IotHubConnectionCredentials : IConnectionCredentials
    {
        /// <summary>
        /// Creates an instance of this class based on an authentication method, the host name of the IoT hub and an optional gateway host name.
        /// </summary>
        /// <param name="authenticationMethod">The authentication method that is used.</param>
        /// <param name="hostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="gatewayHostName">The fully-qualified DNS host name of the gateway (optional).</param>
        /// <returns>A new instance of the <see cref="IotHubConnectionCredentials"/> class with a populated connection string.</returns>
        public IotHubConnectionCredentials(IAuthenticationMethod authenticationMethod, string hostName, string gatewayHostName = null)
        {
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));

            IotHubHostName = hostName;
            IsUsingGateway = !gatewayHostName.IsNullOrWhiteSpace();
            GatewayHostName = IsUsingGateway
                ? gatewayHostName
                : IotHubHostName;

            AuthenticationMethod = authenticationMethod;
            AuthenticationMethod.Populate(this);
            SetTokenRefresherIfApplicable();

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
            SetTokenRefresherIfApplicable();

            Validate();
        }

        /// <summary>
        /// The fully-qualified DNS hostname of the IoT hub service.
        /// </summary>
        public string IotHubHostName { get; private set; }

        /// <summary>
        /// The host service that this client connects to.
        /// This can either be the IoT hub name or a gateway service name.
        /// </summary>
        public string GatewayHostName { get; private set; }

        /// <summary>
        /// Indicates if the client is connecting to IoT hub service through a gateway service.
        /// </summary>
        public bool IsUsingGateway { get; private set; }

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
        public TimeSpan SasTokenTimeToLive { get; internal set; }

        /// <summary>
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// </summary>
        public int SasTokenRenewalBuffer { get; internal set; }

        /// <summary>
        /// The token refresh logic to be used for clients authenticating with either an AuthenticationWithTokenRefresh IAuthenticationMethod mechanism
        /// or through a shared access key value that can be used by the SDK to generate SAS tokens.
        /// </summary>
        public AuthenticationWithTokenRefresh SasTokenRefresher { get; private set; }

        /// <summary>
        /// The authentication method to be used with the IoT hub service.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod { get; private set; }

        async Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(IotHubConnectionCredentials)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");

                Debug.Assert(
                    !SharedAccessSignature.IsNullOrWhiteSpace()
                        || SasTokenRefresher != null,
                    "The token refresher and the shared access signature can't both be null");

                if (!SharedAccessSignature.IsNullOrWhiteSpace())
                {
                    return SharedAccessSignature;
                }

                return SasTokenRefresher == null
                    ? null
                    : await SasTokenRefresher.GetTokenAsync(IotHubHostName);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(IotHubConnectionCredentials)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");
            }
        }

        /// <summary>
        /// This overridden Equals implementation is being referenced when fetching the client identity (AmqpUnit)
        /// from an AMQP connection pool with multiplexed client connections.
        /// This implementation only uses device Id, hostname and module Id when evaluating equality.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is IotHubConnectionCredentials connectionCredentials
                && GetHashCode() == connectionCredentials.GetHashCode()
                && Equals(DeviceId, connectionCredentials.DeviceId)
                && Equals(GatewayHostName, connectionCredentials.GatewayHostName)
                && Equals(ModuleId, connectionCredentials.ModuleId);
        }

        /// <summary>
        /// This hashing algorithm is used in two places:
        /// - when fetching the object hashcode for our logging implementation
        /// - when fetching the client identity (AmqpUnit) from an AMQP connection pool with multiplexed client connections
        /// This algorithm only uses device Id, hostname and module Id when evaluating the hash.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, DeviceId);
            hashCode = UpdateHashCode(hashCode, GatewayHostName);
            hashCode = UpdateHashCode(hashCode, ModuleId);
            return hashCode;
        }

        private static int UpdateHashCode(int hashCode, object field)
        {
            return field == null
                ? hashCode
                : hashCode * -1521134295 + field.GetHashCode();
        }

        private void PopulatePropertiesFromConnectionString(IotHubConnectionString iotHubConnectionString)
        {
            IotHubHostName = iotHubConnectionString.HostName;
            IsUsingGateway = !iotHubConnectionString.GatewayHostName.IsNullOrWhiteSpace();
            GatewayHostName = IsUsingGateway
                ? iotHubConnectionString.GatewayHostName
                : IotHubHostName;
            DeviceId = iotHubConnectionString.DeviceId;
            ModuleId = iotHubConnectionString.ModuleId;
            SharedAccessKeyName = iotHubConnectionString.SharedAccessKeyName;
            SharedAccessKey = iotHubConnectionString.SharedAccessKey;
            SharedAccessSignature = iotHubConnectionString.SharedAccessSignature;
        }

        private void SetTokenRefresherIfApplicable()
        {
            if (AuthenticationMethod is AuthenticationWithTokenRefresh authWithTokenRefresh)
            {
                SasTokenRefresher = authWithTokenRefresh;

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(AuthenticationWithTokenRefresh)}: {Logging.IdOf(SasTokenRefresher)}");

                Debug.Assert(SasTokenRefresher != null);
            }
            else if (!SharedAccessKey.IsNullOrWhiteSpace())
            {
                if (ModuleId.IsNullOrWhiteSpace())
                {
                    // Since the SDK creates the instance of disposable DeviceAuthenticationWithSakRefresh, the SDK needs to
                    // dispose it once the client is disposed.
                    SasTokenRefresher = new DeviceAuthenticationWithSakRefresh(
                        DeviceId,
                        SharedAccessKey,
                        SharedAccessKeyName,
                        SasTokenTimeToLive,
                        SasTokenRenewalBuffer,
                        disposeWithClient: true);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(DeviceAuthenticationWithSakRefresh)}: {Logging.IdOf(SasTokenRefresher)}");
                }
                else
                {
                    // Since the SDK creates the instance of disposable ModuleAuthenticationWithSakRefresh, the SDK needs to
                    // dispose it once the client is disposed.
                    SasTokenRefresher = new ModuleAuthenticationWithSakRefresh(
                        DeviceId,
                        ModuleId,
                        SharedAccessKey,
                        SharedAccessKeyName,
                        SasTokenTimeToLive,
                        SasTokenRenewalBuffer,
                        disposeWithClient: true);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ModuleAuthenticationWithSakRefresh)}: {Logging.IdOf(SasTokenRefresher)}");
                }

                // This assignment resets any previously set SharedAccessSignature value. This is possible in flows where the same authentication method instance
                // is used to reinitialize the client after close-dispose.
                // SharedAccessSignature should be set only if it is non-null and the authentication method of the device client is
                // not of type AuthenticationWithTokenRefresh.
                // Setting the SAS value for an AuthenticationWithTokenRefresh authentication type will result in tokens not being renewed.
                // This flow can be hit if the same authentication method is always used to initialize the client;
                // as in, on disposal and reinitialization. This is because the value of the SAS token computed is stored within the authentication method,
                // and on reinitialization the client is incorrectly identified as a fixed-sas-token-initialized client,
                // instead of being identified as a sas-token-refresh-enabled-client.
                SharedAccessSignature = null;

                Debug.Assert(SasTokenRefresher != null);
            }
        }

        private void Validate()
        {
            // IoT Hub host name
            if (IotHubHostName.IsNullOrWhiteSpace())
            {
                throw new FormatException("IoT hub service hostname to connect to must be specified.");
            }

            // Gateway host name
            if (GatewayHostName.IsNullOrWhiteSpace())
            {
                throw new FormatException("Gateway service hostname to connect to must be specified.");
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
