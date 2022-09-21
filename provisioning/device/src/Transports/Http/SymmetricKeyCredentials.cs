// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class SymmetricKeyCredentials : ServiceClientCredentials
    {
        private const string SASHeaderName = "SharedAccessSignature";
        private readonly string SymmetricKey;
        private volatile string _sasToken;

        public SymmetricKeyCredentials(string symmetricKey) : base()
        {
            SymmetricKey = symmetricKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string audience = request.RequestUri.AbsolutePath.Trim('/');
            string[] segments = audience.Split('/');

            string target = string.Concat(segments[0], '/', segments[1], '/', segments[2]);
            _sasToken = ProvisioningSasBuilder.BuildSasSignature(SymmetricKey, target, TimeSpan.FromDays(1));
            SetAuthorizationHeader(request, _sasToken);

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        private static void SetAuthorizationHeader(HttpRequestMessage request, string sasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(SASHeaderName, sasToken.Substring(SASHeaderName.Length + 1));
        }
    }
}
