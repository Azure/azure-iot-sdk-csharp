// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed partial class IotHubConnectionString : IAuthorizationProvider
    {
        private const int DefaultSecurePort = 5671;

        public IotHubConnectionString(IotHubConnectionStringBuilder builder)
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

            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, HostName, DefaultSecurePort).Uri;

            if (builder.AuthenticationMethod is AuthenticationWithTokenRefresh)
            {
                TokenRefresher = (AuthenticationWithTokenRefresh)builder.AuthenticationMethod;
                if (Logging.IsEnabled)
                {
                    Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(AuthenticationWithTokenRefresh)}: {Logging.IdOf(TokenRefresher)}");
                }

                if (Logging.IsEnabled)
                {
                    Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));
                }

                Debug.Assert(TokenRefresher != null);
            }
            else if (!string.IsNullOrEmpty(SharedAccessKey))
            {
                if (ModuleId.IsNullOrWhiteSpace())
                {
                    // Since the sdk creates the instance of disposable DeviceAuthenticationWithSakRefresh, the sdk needs to dispose it once the client is disposed.
                    TokenRefresher = new DeviceAuthenticationWithSakRefresh(DeviceId, this, builder.SasTokenTimeToLive, builder.SasTokenRenewalBuffer, disposeWithClient: true);

                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(DeviceAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                    }
                }
                else
                {
                    // Since the sdk creates the instance of disposable ModuleAuthenticationWithSakRefresh, the sdk needs to dispose it once the client is disposed.
                    TokenRefresher = new ModuleAuthenticationWithSakRefresh(DeviceId, ModuleId, this, builder.SasTokenTimeToLive, builder.SasTokenRenewalBuffer, disposeWithClient: true);

                    if (Logging.IsEnabled)
                    {
                        Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ModuleAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                    }
                }

                if (Logging.IsEnabled)
                {
                    Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));
                }

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

        // This constructor is only used for unit testing.
        internal IotHubConnectionString(
            string ioTHubName = null,
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
            IotHubName = ioTHubName;
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
    }
}
