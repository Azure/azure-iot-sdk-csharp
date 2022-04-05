// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.Devices.Client.HsmAuthentication.Transport;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    internal static class HttpClientHelper
    {
        private const string HttpScheme = "http";
        private const string HttpsScheme = "https";
        private const string UnixScheme = "unix";

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "The caller of this method is in charge of disposing the HTTP client that is returned.")]
        public static HttpClient GetHttpClient(Uri providerUri)
        {
            HttpClient client;

            if (providerUri.Scheme.Equals(HttpScheme, StringComparison.OrdinalIgnoreCase)
                | providerUri.Scheme.Equals(HttpsScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient();
                return client;
            }

            if (providerUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient(new HttpUdsMessageHandler(providerUri));
                return client;
            }

            throw new InvalidOperationException("ProviderUri scheme is not supported");
        }

        public static string GetBaseUrl(Uri providerUri)
        {
            if (providerUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                return $"{HttpScheme}://{providerUri.Segments.Last()}";
            }

            return providerUri.OriginalString;
        }
    }
}
