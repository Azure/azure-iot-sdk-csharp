// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Net;

#if !NETMF
    using Microsoft.Azure.Amqp;
    using System.Threading.Tasks;
#endif

    using Microsoft.Azure.Devices.Client.Extensions;

    internal sealed partial class IotHubConnectionString : IAuthorizationProvider
#if !NETMF
        , ICbsTokenProvider
#endif
    {
        const string UserSeparator = "@";

        public IotHubConnectionString(IotHubConnectionStringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            this.Audience = builder.HostName;
            this.HostName = builder.GatewayHostName == null || builder.GatewayHostName == "" ? builder.HostName : builder.GatewayHostName;
            this.SharedAccessKeyName = builder.SharedAccessKeyName;
            this.SharedAccessKey = builder.SharedAccessKey;
            this.SharedAccessSignature = builder.SharedAccessSignature;
            this.IotHubName = builder.IotHubName;
            this.DeviceId = builder.DeviceId;
            this.ModuleId = builder.ModuleId;

#if WINDOWS_UWP || PCL || NETSTANDARD1_3
            this.HttpsEndpoint = new UriBuilder("https", this.HostName).Uri;
#elif !NETMF
            this.HttpsEndpoint = new UriBuilder(Uri.UriSchemeHttps, this.HostName).Uri;
#elif NETMF
            this.HttpsEndpoint = new Uri("https://" + this.HostName);
#endif

#if !NETMF
            this.AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, this.HostName, AmqpConstants.DefaultSecurePort).Uri;

            if (builder.AuthenticationMethod is AuthenticationWithTokenRefresh)
            {
                this.TokenRefresher = (AuthenticationWithTokenRefresh)builder.AuthenticationMethod;
            }
            else if (!string.IsNullOrEmpty(this.SharedAccessKey))
            {
                this.TokenRefresher = this.ModuleId.IsNullOrWhiteSpace()
                    ? new DeviceAuthenticationWithSakRefresh(this.DeviceId, this)
                    : new ModuleAuthenticationWithSakRefresh(this.DeviceId, this.ModuleId, this) as AuthenticationWithTokenRefresh;
            }
#endif
        }

        public string IotHubName
        {
            get;
            private set;
        }

        public string DeviceId
        {
            get;
            private set;
        }

        public string ModuleId
        {
            get;
            private set;
        }

        public string HostName
        {
            get;
            private set;
        }

        public Uri HttpsEndpoint
        {
            get;
            private set;
        }

#if !NETMF
        public Uri AmqpEndpoint
        {
            get;
            private set;
        }
#endif

        public string Audience
        {
            get;
            private set;
        }

        public string SharedAccessKeyName
        {
            get;
            private set;
        }

        public string SharedAccessKey
        {
            get;
            private set;
        }

        public string SharedAccessSignature
        {
            get;
            private set;
        }
    }
}
