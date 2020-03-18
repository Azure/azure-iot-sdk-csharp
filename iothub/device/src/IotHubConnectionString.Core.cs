// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed partial class IotHubConnectionString : IAuthorizationProvider
    {
        public AuthenticationWithTokenRefresh TokenRefresher
        {
            get;
            private set;
        }

        Task<string> IAuthorizationProvider.GetPasswordAsync()
        {
            try
            {
                if (Logging.IsEnabled) Logging.Enter(this, $"{nameof(IotHubConnectionString)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");

                Debug.Assert(this.TokenRefresher != null);

                if (!string.IsNullOrWhiteSpace(this.SharedAccessSignature))
                {
                    return Task.FromResult(this.SharedAccessSignature);
                }

                return this.TokenRefresher.GetTokenAsync(this.Audience);
            }
            finally
            {
                if (Logging.IsEnabled) Logging.Exit(this, $"{nameof(IotHubConnectionString)}.{nameof(IAuthorizationProvider.GetPasswordAsync)}");
            }
        }

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(this.AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }
    }
}
