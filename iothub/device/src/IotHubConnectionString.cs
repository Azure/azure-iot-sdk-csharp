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
        private const string UserSeparator = "@";
        private const int DefaultSecurePort = 5671;

        public IotHubConnectionString(IotHubConnectionStringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Audience = builder.HostName;
            HostName = string.IsNullOrEmpty(builder.GatewayHostName)
                ? builder.HostName
                : builder.GatewayHostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            SharedAccessSignature = builder.SharedAccessSignature;
            IotHubName = builder.IotHubName;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;

            HttpsEndpoint = new UriBuilder(Uri.UriSchemeHttps, HostName).Uri;

            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, HostName, DefaultSecurePort).Uri;

            if (builder.AuthenticationMethod is AuthenticationWithTokenRefresh)
            {
                TokenRefresher = (AuthenticationWithTokenRefresh)builder.AuthenticationMethod;
                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(AuthenticationWithTokenRefresh)}: {Logging.IdOf(TokenRefresher)}");
                if (Logging.IsEnabled) Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));
                Debug.Assert(TokenRefresher != null);
            }
            else if (!string.IsNullOrEmpty(SharedAccessKey))
            {
                if (ModuleId.IsNullOrWhiteSpace())
                {
                    TokenRefresher = new DeviceAuthenticationWithSakRefresh(DeviceId, this) as AuthenticationWithTokenRefresh;
                    if (Logging.IsEnabled) Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(DeviceAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                }
                else
                {
                    TokenRefresher = new ModuleAuthenticationWithSakRefresh(DeviceId, ModuleId, this) as AuthenticationWithTokenRefresh;
                    if (Logging.IsEnabled) Logging.Info(this, $"{nameof(IAuthenticationMethod)} is {nameof(ModuleAuthenticationWithSakRefresh)}: {Logging.IdOf(TokenRefresher)}");
                }

                if (Logging.IsEnabled) Logging.Associate(this, TokenRefresher, nameof(TokenRefresher));
                Debug.Assert(TokenRefresher != null);
            }
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
    }
}
