// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class SymmetricKeyCredentials : ServiceClientCredentials
    {
        private const string SasHeaderName = "SharedAccessSignature";
        private const string Registration = "registration";

        private readonly string _symmetricKey;

        public SymmetricKeyCredentials(string symmetricKey)
            : base()
        {
            _symmetricKey = symmetricKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string audience = request.RequestUri.AbsolutePath.Trim('/');
            string[] segments = audience.Split('/');

            string sasToken = ProvisioningSasBuilder.BuildSasSignature(
                Registration,
                _symmetricKey,
                // These values may come in encoded, so decode them for the SAS token
                $"{WebUtility.UrlDecode(segments[0])}/{WebUtility.UrlDecode(segments[1])}/{WebUtility.UrlDecode(segments[2])}",
                TimeSpan.FromDays(1));
            SetAuthorizationHeader(request, sasToken);

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        private static void SetAuthorizationHeader(HttpRequestMessage request, string sasToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(SasHeaderName, sasToken.Substring(SasHeaderName.Length + 1));
        }
    }
}