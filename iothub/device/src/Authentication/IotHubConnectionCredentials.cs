// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Holder for client credentials that will be used for authenticating the client with IoT hub service.
    /// </summary>
    public sealed class IotHubConnectionCredentials : IConnectionCredentials
    {
        // for mocking purposes only
        internal IotHubConnectionCredentials()
        {
        }

        /// <summary>
        /// Creates an instance of this class based on an authentication method, the host name of the IoT hub and an optional gateway host name.
        /// </summary>
        /// <param name="authenticationMethod">
        /// The authentication method that is used. It includes <see cref="ClientAuthenticationWithSharedAccessKeyRefresh"/>, <see cref="ClientAuthenticationWithSharedAccessSignature"/>,
        /// <see cref="ClientAuthenticationWithX509Certificate"/> or <see cref="EdgeModuleAuthenticationWithHsm"/>.
        /// </param>
        /// <param name="iotHubHostName">The fully-qualified DNS host name of IoT hub.</param>
        /// <param name="gatewayHostName">The fully-qualified DNS host name of the gateway (optional).</param>
        /// <returns>A new instance of the <c>IotHubConnectionCredentials</c> class with a populated connection string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="iotHubHostName"/>, device Id or <paramref name="authenticationMethod"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="iotHubHostName"/> or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="ArgumentException"><see cref="ClientAuthenticationWithX509Certificate.ChainCertificates"/> is used over a protocol other than MQTT over TCP or AMQP over TCP.</exception>
        /// <exception cref="FormatException">Neither shared access key, shared access signature or X509 certificates were presented for authentication.</exception>
        /// <exception cref="FormatException">Either shared access key or shared access signature where presented together with X509 certificates for authentication.</exception>
        /// <exception cref="IotHubClientException"><see cref="ClientAuthenticationWithX509Certificate.ChainCertificates"/> could not be installed.</exception>
        public IotHubConnectionCredentials(IAuthenticationMethod authenticationMethod, string iotHubHostName, string gatewayHostName = null)
        {
            Argument.AssertNotNull(authenticationMethod, nameof(authenticationMethod));
            Argument.AssertNotNullOrWhiteSpace(iotHubHostName, nameof(iotHubHostName));

            IotHubHostName = iotHubHostName;
            GatewayHostName = gatewayHostName;
            HostName = gatewayHostName ?? iotHubHostName;

            AuthenticationMethod = authenticationMethod;
            IotHubConnectionCredentials iotHubConnectionCredentials = this;
            AuthenticationMethod.Populate(ref iotHubConnectionCredentials);
            SetAuthenticationModel();
            SetTokenRefresherIfApplicable();

            Validate();
        }

        /// <summary>
        /// Creates an instance of this class using a connection string.
        /// </summary>
        /// <param name="iotHubConnectionString">The IoT hub device connection string.</param>
        /// <returns>A new instance of this class.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="iotHubConnectionString"/>, IoT hub host name or device Id is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="iotHubConnectionString"/>, IoT hub host name or device Id are an empty string or consist only of white-space characters.</exception>
        /// <exception cref="FormatException">Neither shared access key nor shared access signature were presented for authentication.</exception>
        /// <exception cref="FormatException">Either shared access key or shared access signature where presented together with X509 certificates for authentication.</exception>
        public IotHubConnectionCredentials(string iotHubConnectionString)
        {
            Argument.AssertNotNullOrWhiteSpace(iotHubConnectionString, nameof(iotHubConnectionString));

            // We'll parse the connection string and use that to build an auth method
            IotHubConnectionString parsedConnectionString = IotHubConnectionStringParser.Parse(iotHubConnectionString);
            AuthenticationMethod = GetAuthenticationMethodFromConnectionString(parsedConnectionString);

            PopulatePropertiesFromConnectionString(parsedConnectionString);
            SetAuthenticationModel();
            SetTokenRefresherIfApplicable();

            Validate();
        }

        /// <summary>
        /// The fully-qualified DNS host name of the IoT hub service.
        /// </summary>
        public string IotHubHostName { get; private set; }

        /// <summary>
        /// The optional name of the gateway service to connect to.
        /// </summary>
        public string GatewayHostName { get; private set; }

        /// <summary>
        /// The host service that this client connects to.
        /// This can either be the IoT hub name or a gateway service name.
        /// </summary>
        public string HostName { get; private set; }

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
        /// The token refresh logic to be used for clients authenticating with either an ClientAuthenticationWithTokenRefresh IAuthenticationMethod mechanism
        /// or through a shared access key value that can be used by the SDK to generate SAS tokens.
        /// </summary>
        public ClientAuthenticationWithTokenRefresh SasTokenRefresher { get; private set; }

        /// <summary>
        /// The authentication method to be used with the IoT hub service.
        /// </summary>
        public IAuthenticationMethod AuthenticationMethod { get; private set; }

        /// <summary>
        /// The authentication model for the device; i.e. X.509 certificates, individual client scoped SAS tokens or IoT hub level scoped SAS tokens.
        /// </summary>
        public AuthenticationModel AuthenticationModel { get; private set; }

        /// <summary>
        /// Gets the SAS token credential required for authenticating the client with IoT hub service.
        /// </summary>
        async Task<string> IConnectionCredentials.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(IotHubConnectionCredentials)}.{nameof(IConnectionCredentials.GetPasswordAsync)}");

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
                    Logging.Exit(this, $"{nameof(IotHubConnectionCredentials)}.{nameof(IConnectionCredentials.GetPasswordAsync)}");
            }
        }

        /// <summary>
        /// This overridden Equals implementation is being referenced when fetching the client identity (AmqpUnit)
        /// from an AMQP connection pool with multiplexed client connections.
        /// This implementation only uses device Id, host name, module Id and the authentication model when evaluating equality.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is IotHubConnectionCredentials connectionCredentials
                && GetHashCode() == connectionCredentials.GetHashCode()
                && Equals(DeviceId, connectionCredentials.DeviceId)
                && Equals(HostName, connectionCredentials.HostName)
                && Equals(ModuleId, connectionCredentials.ModuleId)
                && Equals(AuthenticationModel, connectionCredentials.AuthenticationModel);
        }

        /// <summary>
        /// This hashing algorithm is used in two places:
        /// - when fetching the object hashcode for our logging implementation
        /// - when fetching the client identity (AmqpUnit) from an AMQP connection pool with multiplexed client connections
        /// This algorithm only uses device Id, host name, module Id and the authentication model when evaluating the hash.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, DeviceId);
            hashCode = UpdateHashCode(hashCode, HostName);
            hashCode = UpdateHashCode(hashCode, ModuleId);
            hashCode = UpdateHashCode(hashCode, AuthenticationModel);
            return hashCode;
        }

        // Edge Modules and Module Twins have different links to be used for the same function when communicating over AMQP
        // We are setting the flag on these methods since the decision should be made at the transport layer and not at the
        // client layer.
        //
        // This means that all other transports will need to implement this method. However they do not need to use the flag
        // if there is no behavior change required.
        internal bool IsEdgeModule => !string.IsNullOrWhiteSpace(GatewayHostName);

        private static int UpdateHashCode(int hashCode, object field)
        {
            return field == null
                ? hashCode
                : hashCode * -1521134295 + field.GetHashCode();
        }

        private void PopulatePropertiesFromConnectionString(IotHubConnectionString iotHubConnectionString)
        {
            IotHubHostName = iotHubConnectionString.IotHubHostName;
            GatewayHostName = iotHubConnectionString.GatewayHostName;
            HostName = GatewayHostName ?? IotHubHostName;
            DeviceId = iotHubConnectionString.DeviceId;
            ModuleId = iotHubConnectionString.ModuleId;
            SharedAccessKeyName = iotHubConnectionString.SharedAccessKeyName;
            SharedAccessKey = iotHubConnectionString.SharedAccessKey;
            SharedAccessSignature = iotHubConnectionString.SharedAccessSignature;
            Certificate = null;
            ChainCertificates = null;
        }

        private void SetTokenRefresherIfApplicable()
        {
            if (AuthenticationMethod is ClientAuthenticationWithTokenRefresh authWithTokenRefresh)
            {
                SasTokenRefresher = authWithTokenRefresh;

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ClientAuthenticationWithTokenRefresh)}: {Logging.IdOf(SasTokenRefresher)}");

                Debug.Assert(SasTokenRefresher != null);
            }
            else if (!SharedAccessKey.IsNullOrWhiteSpace())
            {
                SasTokenRefresher = new ClientAuthenticationWithSharedAccessKeyRefresh(
                    SharedAccessKey,
                    DeviceId,
                    ModuleId,
                    SharedAccessKeyName,
                    SasTokenTimeToLive,
                    SasTokenRenewalBuffer);

                if (Logging.IsEnabled)
                    Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ClientAuthenticationWithSharedAccessKeyRefresh)}: {Logging.IdOf(SasTokenRefresher)}");

                // This assignment resets any previously set SharedAccessSignature value. This is possible in flows where the same authentication method instance
                // is used to reinitialize the client after close-dispose.
                // SharedAccessSignature should be set only if it is non-null and the authentication method of the device client is
                // not of type ClientAuthenticationWithTokenRefresh.
                // Setting the SAS value for an ClientAuthenticationWithTokenRefresh authentication type will result in tokens not being renewed.
                // This flow can be hit if the same authentication method is always used to initialize the client;
                // as in, on disposal and reinitialization. This is because the value of the SAS token computed is stored within the authentication method,
                // and on reinitialization the client is incorrectly identified as a fixed-sas-token-initialized client,
                // instead of being identified as a sas-token-refresh-enabled-client.
                SharedAccessSignature = null;

                Debug.Assert(SasTokenRefresher != null);
            }
        }

        private void SetAuthenticationModel()
        {
            AuthenticationModel = Certificate == null
                ? SharedAccessKeyName == null
                    ? AuthenticationModel.SasIndividual
                    : AuthenticationModel.SasGrouped
                : AuthenticationModel.X509;
        }

        private void Validate()
        {
            // IoT Hub host name
            if (string.IsNullOrWhiteSpace(IotHubHostName))
            {
                throw new FormatException("Missing IoT hub host name.");
            }

            // Host name
            if (string.IsNullOrWhiteSpace(HostName))
            {
                throw new FormatException("Missing host name.");
            }

            // Device Id
            if (string.IsNullOrWhiteSpace(DeviceId))
            {
                throw new FormatException("Device Id cannot be null or white space.");
            }

            // Module Id
            if (ModuleId != null && string.IsNullOrWhiteSpace(ModuleId))
            {
                // A module Id is not required to provide, but if they do it must be a valid string.
                throw new FormatException("Module Id cannot be white space.");
            }

            // Shared access key
            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
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

            // Either shared access key, shared access signature or X.509 certificate is required for authenticating the client with IoT hub.
            // These values should be populated in the constructor. The only exception to this scenario is when the authentication method is
            // ClientAuthenticationWithTokenRefresh, in which case the shared access signature is initially null and is generated on demand during client authentication.
            if (Certificate == null
                && SharedAccessKey.IsNullOrWhiteSpace()
                && SharedAccessSignature.IsNullOrWhiteSpace()
                && AuthenticationMethod is not ClientAuthenticationWithTokenRefresh)
            {
                throw new FormatException(
                        "Should specify either SharedAccessKey, SharedAccessSignature or X.509 certificate for authenticating the client with IoT hub.");
            }

            // If an X.509 certificate is supplied then neither shared access key nor shared access signature should be supplied.
            if (Certificate != null
                && (!SharedAccessKey.IsNullOrWhiteSpace()
                    || !SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new FormatException(
                    "Should not specify either SharedAccessKey or SharedAccessSignature if X.509 certificate is used for authenticating the client with IoT hub.");
            }

            // Validate certs.
            if (AuthenticationMethod is ClientAuthenticationWithX509Certificate)
            {
                // Prep for certificate auth.
                if (Certificate == null)
                {
                    throw new FormatException("No certificate was found. To use certificate authentication certificate must be present.");
                }

                if (ChainCertificates != null)
                {
                    // Install all the intermediate certificates in the chain if specified.
                    try
                    {
                        CertificateInstaller.EnsureChainIsInstalled(ChainCertificates);
                    }
                    catch (Exception ex)
                    {
                        if (Logging.IsEnabled)
                            Logging.Error(null, $"{nameof(CertificateInstaller)} failed to read or write to cert store due to: {ex}");

                        throw new IotHubClientException($"Failed to provide certificates in the chain - {ex.Message}", IotHubClientErrorCode.Unauthorized, ex);
                    }
                }
            }

            if (SasTokenTimeToLive > TimeSpan.FromHours(1))
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, $"You are using a larger value {SasTokenTimeToLive} than our recommendation 1 hour for the SAS token TTL.");
            }

            if (SasTokenRenewalBuffer > 85)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, $"You are using a larger percent of TTL {SasTokenRenewalBuffer} as the token renewal buffer. " +
                        "Your token will be renewed more frequently.");
            }
        }

        private static IAuthenticationMethod GetAuthenticationMethodFromConnectionString(IotHubConnectionString iotHubConnectionString)
        {
            if (iotHubConnectionString.SharedAccessKey != null)
            {
                return new ClientAuthenticationWithSharedAccessKeyRefresh(
                        iotHubConnectionString.SharedAccessKey,
                        iotHubConnectionString.DeviceId,
                        iotHubConnectionString.ModuleId,
                        iotHubConnectionString.SharedAccessKeyName);
            }
            else if (iotHubConnectionString.SharedAccessSignature != null)
            {
                return new ClientAuthenticationWithSharedAccessSignature(
                    iotHubConnectionString.SharedAccessSignature,
                    iotHubConnectionString.DeviceId,
                    iotHubConnectionString.ModuleId);
            }

            throw new FormatException($"Should specify either SharedAccessKey or SharedAccessSignature" +
                $" if connection string is used for authenticating the client with IoT hub.");
        }
    }
}
