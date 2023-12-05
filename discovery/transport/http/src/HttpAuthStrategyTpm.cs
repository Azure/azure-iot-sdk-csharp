// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Microsoft.Azure.Devices.Discovery.Client.Transport.Http;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    internal class HttpAuthStrategyTpm : HttpAuthStrategy
    {
        private SecurityProviderTpm _security;

        public HttpAuthStrategyTpm(SecurityProviderTpm security)
        {
            _security = security;
        }

        public override MicrosoftFairfieldGardensDiscovery CreateClient(Uri uri, HttpClientHandler httpClientHandler)
        {
            var apiVersionDelegatingHandler = new ApiVersionDelegatingHandler();

            var dpsClient = new MicrosoftFairfieldGardensDiscovery(
                uri,
                httpClientHandler,
                apiVersionDelegatingHandler);

            return dpsClient;
        }
    }
}
