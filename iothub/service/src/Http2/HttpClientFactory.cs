// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Http2
{
    /// <summary>
    /// Factory for creating HTTP clients for the various service clients to use when making
    /// HTTP requests to the service. Each service client should only need to create one HTTP client
    /// for its lifetime.
    /// </summary>
    internal class HttpClientFactory
    {
        private const string HttpsEndpointPrefix = "https";

        /// <summary>
        /// Create an HTTP client for communicating with the provided host and that uses the
        /// provided settings.
        /// </summary>
        /// <remarks>
        /// If the provided settings contains an HTTP client, then this function will return
        /// that HTTP client instead of creating a new one.
        /// </remarks>
        /// <param name="hostName">The host name of the IoT hub this client will send requests to.</param>
        /// <param name="settings">The optional settings for this client to use.</param>
        /// <returns>The created HTTP client.</returns>
        internal static HttpClient Create(string hostName, HttpTransportSettings2 settings)
        {
            Uri httpsEndpoint = new UriBuilder(HttpsEndpointPrefix, hostName).Uri;

            if (settings.HttpClient != null)
            {
                Uri providedEndpoint = settings.HttpClient.BaseAddress;
                if (!providedEndpoint.Equals(httpsEndpoint))
                {
                    throw new ArgumentException($"The provided HTTP client targets a different URI than expected. Expected: {httpsEndpoint}, Actual: {providedEndpoint}");
                }

                return settings.HttpClient;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope.
            // This handler is used within the returned HttpClient, so it cannot be disposed within this scope.
            var httpMessageHandler = new HttpClientHandler();
#pragma warning restore CA2000 // Dispose objects before losing scope
            httpMessageHandler.SslProtocols = TlsVersions.Instance.Preferred;
            httpMessageHandler.CheckCertificateRevocationList = TlsVersions.Instance.CertificateRevocationCheck;

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
