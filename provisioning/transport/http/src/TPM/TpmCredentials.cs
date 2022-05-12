// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class TpmCredentials : ServiceClientCredentials
    {
        private const string SASHeaderName = "SharedAccessSignature";
        private volatile string _sasToken;

        public TpmCredentials() : base()
        {
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_sasToken))
            {
                Action<string> action = (value) =>
                {
#if NETSTANDARD2_1
                    _sasToken = value.Replace(SASHeaderName + " ", "", StringComparison.Ordinal);
#else
                    _sasToken = value.Replace(SASHeaderName + " ", "");
#endif
                    SetAuthorizationHeader(request, _sasToken);
                };

#if NET5_0_OR_GREATER
                HttpRequestOptions requestOptions = request.Options;
                var requestOptionsKey = new HttpRequestOptionsKey<Action<string>>(TpmDelegatingHandler.ProvisioningHeaderName);
                requestOptions.Set(requestOptionsKey, action);
#else
                request.Properties.Add(TpmDelegatingHandler.ProvisioningHeaderName, action);
#endif
            }
            else
            {
                SetAuthorizationHeader(request, _sasToken);
            }

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        private static void SetAuthorizationHeader(HttpRequestMessage request, string sasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(SASHeaderName, sasToken);
        }
    }
}
