// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Transport;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientConfiguration : IClientConfiguration
    {
        public ClientConfiguration(
            IotHubConnectionStringBuilder builder,
            IotHubClientOptions iotHubClientOptions)
        {
            Argument.AssertNotNull(builder, nameof(builder));

            // Frist validate that the IotHubConnectionStringBuilder is set with the expected fields.
            builder.Validate();

            IotHubHostName = builder.HostName;
            IsUsingGateway = !string.IsNullOrEmpty(builder.GatewayHostName);
            GatewayHostName = IsUsingGateway
                ? builder.GatewayHostName
                : builder.HostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;

            ClientOptions = iotHubClientOptions;

            if (builder.AuthenticationMethod is AuthenticationWithTokenRefresh authWithTokenRefresh)
            {
                TokenRefresher = authWithTokenRefresh;
                if (Logging.IsEnabled)
                {
                    Logging.Info(
                        this,
                        $"{nameof(IAuthenticationMethod)} is {nameof(AuthenticationWithTokenRefresh)}: {Logging.IdOf(TokenRefresher)}");
                    Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));
                }

                Debug.Assert(TokenRefresher != null);
            }
            else if (!string.IsNullOrEmpty(SharedAccessKey))
            {
                if (ModuleId.IsNullOrWhiteSpace())
                {
                    // Since the SDK creates the instance of disposable DeviceAuthenticationWithSakRefresh, the SDK needs to
                    // dispose it once the client is disposed.
                    TokenRefresher = new DeviceAuthenticationWithSakRefresh(
                        DeviceId,
                        this,
                        builder.SasTokenTimeToLive,
                        builder.SasTokenRenewalBuffer,
                        disposeWithClient: true);

                    if (Logging.IsEnabled)
                        Logging.Info(
                            this,
                            $"{nameof(IAuthenticationMethod)} is {nameof(DeviceAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                }
                else
                {
                    // Since the SDK creates the instance of disposable ModuleAuthenticationWithSakRefresh, the SDK needs to
                    // dispose it once the client is disposed.
                    TokenRefresher = new ModuleAuthenticationWithSakRefresh(
                        DeviceId,
                        ModuleId,
                        this,
                        builder.SasTokenTimeToLive,
                        builder.SasTokenRenewalBuffer,
                        disposeWithClient: true);

                    if (Logging.IsEnabled)
                        Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ModuleAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                }

                if (Logging.IsEnabled)
                    Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));

                Debug.Assert(TokenRefresher != null);
            }
            // SharedAccessSignature should be set only if it is non-null and the authentication method of the device client is
            // not of type AuthenticationWithTokenRefresh.
            // Setting the SAS value for an AuthenticationWithTokenRefresh authentication type will result in tokens not being renewed.
            // This flow can be hit if the same authentication method is always used to initialize the client;
            // as in, on disposal and reinitialization. This is because the value of the SAS token computed is stored within the authentication method,
            // and on reinitialization the client is incorrectly identified as a fixed-sas-token-initialized client,
            // instead of being identified as a sas-token-refresh-enabled-client.
            else if (!string.IsNullOrWhiteSpace(builder.SharedAccessSignature))
            {
                SharedAccessSignature = builder.SharedAccessSignature;
            }

            if (ClientOptions.TransportSettings.ClientCertificate == null)
            {
                AuthenticationModel = SharedAccessKeyName == null
                    ? AuthenticationModel.SasIndividual
                    : AuthenticationModel.SasGrouped;
            }
            else
            {
                AuthenticationModel = AuthenticationModel.X509;
            }
        }

        public AuthenticationWithTokenRefresh TokenRefresher { get; }

        public string DeviceId { get; }

        public string ModuleId { get; }

        public string GatewayHostName { get; }

        public string IotHubHostName { get; }

        public string SharedAccessKeyName { get; }

        public string SharedAccessKey { get; }

        public string SharedAccessSignature { get; }

        public bool IsUsingGateway { get; }

        public AuthenticationModel AuthenticationModel { get; }

        public IotHubClientOptions ClientOptions { get; }

        public bool IsPooling()
        {
            return AuthenticationModel != AuthenticationModel.X509
                && ClientOptions.TransportSettings is IotHubClientAmqpSettings iotHubClientAmqpSettings
                && (iotHubClientAmqpSettings?.ConnectionPoolSettings?.Pooling ?? false);
        }

        async Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(ClientConfiguration)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");

                Debug.Assert(
                    !string.IsNullOrWhiteSpace(SharedAccessSignature)
                        || TokenRefresher != null,
                    "The token refresher and the shared access signature can't both be null");

                if (!string.IsNullOrWhiteSpace(SharedAccessSignature))
                {
                    return SharedAccessSignature;
                }

                return TokenRefresher == null
                    ? null
                    : await TokenRefresher.GetTokenAsync(IotHubHostName);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(ClientConfiguration)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");
            }
        }

        /// <summary>
        /// This overridden Equals implementation is being referenced when fetching the client identity (AmqpUnit)
        /// from an AMQP connection pool with multiplexed client connections.
        /// This implementation only uses device Id, hostname, module Id, authentication model and the transport settings protocol type
        /// when evaluating equality.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ClientConfiguration clientConfiguration
                && GetHashCode() == clientConfiguration.GetHashCode()
                && Equals(DeviceId, clientConfiguration.DeviceId)
                && Equals(GatewayHostName, clientConfiguration.GatewayHostName)
                && Equals(ModuleId, clientConfiguration.ModuleId)
                && Equals(ClientOptions.TransportSettings.Protocol, clientConfiguration.ClientOptions.TransportSettings.Protocol)
                && Equals(AuthenticationModel.GetHashCode(), clientConfiguration.AuthenticationModel.GetHashCode());
        }

        /// <summary>
        /// This hashing algorithm is used in two places:
        /// - when fetching the object hashcode for our logging implementation
        /// - when fetching the client identity (AmqpUnit) from an AMQP connection pool with multiplexed client connections
        /// This algorithm only uses device Id, hostname, module Id, authentication model and the transport settings protocol type
        /// when evaluating the hash.
        /// This is the algorithm that was implemented when AMQP connection pooling was first implemented,
        /// so the algorithm has been retained as-is.
        /// </summary>
        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, DeviceId);
            hashCode = UpdateHashCode(hashCode, GatewayHostName);
            hashCode = UpdateHashCode(hashCode, ModuleId);
            hashCode = UpdateHashCode(hashCode, ClientOptions.TransportSettings.Protocol);
            hashCode = UpdateHashCode(hashCode, AuthenticationModel);
            return hashCode;
        }

        private static int UpdateHashCode(int hashCode, object field)
        {
            return field == null
                ? hashCode
                : hashCode * -1521134295 + field.GetHashCode();
        }
    }
}
