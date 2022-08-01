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
    internal class IotHubConnectionInfo : IAuthorizationProvider, IDeviceIdentity
    {
        private const int DefaultAmqpSecurePort = 5671;

        public IotHubConnectionInfo(IotHubConnectionStringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Audience = builder.HostName;
            IsUsingGateway = !string.IsNullOrEmpty(builder.GatewayHostName);
            HostName = IsUsingGateway
                ? builder.GatewayHostName
                : builder.HostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            IotHubName = builder.IotHubName;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;

            HttpsEndpoint = new UriBuilder(Uri.UriSchemeHttps, HostName).Uri;

            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, HostName, DefaultAmqpSecurePort).Uri;

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
            // Setting the sas value for an AuthenticationWithTokenRefresh authentication type will result in tokens not being renewed.
            // This flow can be hit if the same authentication method is always used to initialize the client;
            // as in, on disposal and reinitialization. This is because the value of the sas token computed is stored within the authentication method,
            // and on reinitialization the client is incorrectly identified as a fixed-sas-token-initialized client,
            // instead of being identified as a sas-token-refresh-enabled-client.
            else if (!string.IsNullOrWhiteSpace(builder.SharedAccessSignature))
            {
                SharedAccessSignature = builder.SharedAccessSignature;
            }
        }

        internal IotHubConnectionInfo(
            IotHubClientAmqpSettings amqpTransportSettings,
            ProductInfo productInfo,
            IotHubClientOptions options)
        {
            AmqpTransportSettings = amqpTransportSettings;
            ProductInfo = productInfo;
            Options = options;

            if (amqpTransportSettings.ClientCertificate == null)
            {
                Audience = CreateAudience();
                AuthenticationModel = SharedAccessKeyName == null
                    ? AuthenticationModel.SasIndividual
                    : AuthenticationModel.SasGrouped;
            }
            else
            {
                AuthenticationModel = AuthenticationModel.X509;
            }
        }

        // This constructor is only used for unit testing.
        internal IotHubConnectionInfo(
            string iotHubName = null,
            string deviceId = null,
            string moduleId = null,
            string hostName = null,
            Uri httpsEndpoint = null,
            Uri amqpEndpoint = null,
            string audience = null,
            string sharedAccessKeyName = null,
            string sharedAccessKey = null,
            string sharedAccessSignature = null,
            bool isUsingGateway = false)
        {
            IotHubName = iotHubName;
            DeviceId = deviceId;
            ModuleId = moduleId;
            HostName = hostName;
            HttpsEndpoint = httpsEndpoint;
            AmqpEndpoint = amqpEndpoint;
            Audience = audience;
            SharedAccessKeyName = sharedAccessKeyName;
            SharedAccessKey = sharedAccessKey;
            SharedAccessSignature = sharedAccessSignature;
            IsUsingGateway = isUsingGateway;
        }

        public AuthenticationWithTokenRefresh TokenRefresher { get; private set; }

        public string IotHubName { get; private set; }

        public string DeviceId { get; private set; }

        public string ModuleId { get; private set; }

        public string HostName { get; private set; }

        public Uri HttpsEndpoint { get; private set; }

        public Uri AmqpEndpoint { get; private set; }

        public string Audience { get; private set; }

        public string SharedAccessKeyName { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessSignature { get; private set; }

        public bool IsUsingGateway { get; private set; }

        public AuthenticationModel AuthenticationModel { get; }

        public IotHubClientAmqpSettings AmqpTransportSettings { get; }

        public ProductInfo ProductInfo { get; }

        public IotHubClientOptions Options { get; }

        async Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"{nameof(IotHubConnectionInfo)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");

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
                    : await TokenRefresher.GetTokenAsync(Audience);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(IotHubConnectionInfo)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");
            }
        }

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }

        public bool IsPooling()
        {
            return (AuthenticationModel != AuthenticationModel.X509) && (AmqpTransportSettings?.ConnectionPoolSettings?.Pooling ?? false);
        }

        public override bool Equals(object obj)
        {
            return obj is IotHubConnectionInfo iotHubConnectionInfo
                && GetHashCode() == iotHubConnectionInfo.GetHashCode()
                && Equals(DeviceId, iotHubConnectionInfo.DeviceId)
                && Equals(HostName, iotHubConnectionInfo.HostName)
                && Equals(ModuleId, iotHubConnectionInfo.ModuleId)
                && Equals(AmqpTransportSettings.Protocol, iotHubConnectionInfo.AmqpTransportSettings.Protocol)
                && Equals(AuthenticationModel.GetHashCode(), iotHubConnectionInfo.AuthenticationModel.GetHashCode());
        }

        public override int GetHashCode()
        {
            int hashCode = UpdateHashCode(620602339, DeviceId);
            hashCode = UpdateHashCode(hashCode, HostName);
            hashCode = UpdateHashCode(hashCode, ModuleId);
            hashCode = UpdateHashCode(hashCode, AmqpTransportSettings.Protocol);
            hashCode = UpdateHashCode(hashCode, AuthenticationModel);
            return hashCode;
        }

        private static int UpdateHashCode(int hashCode, object field)
        {
            return field == null
                ? hashCode
                : hashCode * -1521134295 + field.GetHashCode();
        }

        private string CreateAudience()
        {
            if (SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                return ModuleId.IsNullOrWhiteSpace()
                    ? $"{HostName}/devices/{WebUtility.UrlEncode(DeviceId)}"
                    : $"{HostName}/devices/{WebUtility.UrlEncode(DeviceId)}/modules/{WebUtility.UrlEncode(ModuleId)}";
            }
            else
            {
                // this is a group shared key
                return HostName;
            }
        }
    }
}
