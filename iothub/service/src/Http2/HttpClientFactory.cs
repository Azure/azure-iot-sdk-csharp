// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Http2
{
    internal class HttpClientFactory
    {
        private const string HttpsEndpointPrefix = "https";

        internal static HttpClient Create(string hostName, HttpTransportSettings2 settings)
        {
            if (settings.HttpClient != null)
            {
                return settings.HttpClient;
            }

            Uri httpsEndpoint = new UriBuilder(HttpsEndpointPrefix, hostName).Uri;

#if NETCOREAPP && !NETCOREAPP2_0 && !NETCOREAPP1_0 && !NETCOREAPP1_1

#pragma warning disable CA2000 // Dispose objects before losing scope.
            // This handler is used within the returned HttpClient, so it cannot be disposed within this scope.
            // SocketsHttpHandler is only available in netcoreapp2.1 and onwards
            var httpMessageHandler = new SocketsHttpHandler();
#pragma warning restore CA2000 // Dispose objects before losing scope
            httpMessageHandler.SslOptions.EnabledSslProtocols = TlsVersions.Instance.Preferred;
#else
            var httpMessageHandler = new HttpClientHandler();
            httpMessageHandler.SslProtocols = TlsVersions.Instance.Preferred;
            httpMessageHandler.CheckCertificateRevocationList = TlsVersions.Instance.CertificateRevocationCheck;
#endif

            if (settings.Proxy != DefaultWebProxySettings.Instance)
            {
                httpMessageHandler.UseProxy = settings.Proxy != null;
                httpMessageHandler.Proxy = settings.Proxy;
            }

            ServicePointHelpers.SetLimits(httpMessageHandler, httpsEndpoint, settings.ConnectionLeaseTimeoutMilliseconds);

            return new HttpClient(httpMessageHandler);
        }
    }
}
