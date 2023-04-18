// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Factory for creating HTTP clients for the various service clients to use when making
    /// HTTP requests to the service. Each service client should only need to create one HTTP client
    /// for its lifetime.
    /// </summary>
    internal sealed class HttpClientFactory
    {
        internal const string HttpsEndpointPrefix = "https";

        /// <summary>
        /// Create an HTTP client for communicating with the provided host and that uses the
        /// provided settings.
        /// </summary>
        /// <remarks>
        /// If the provided settings contains an HTTP client, then this function will return
        /// that HTTP client instead of creating a new one.
        /// </remarks>
        /// <param name="hostName">The host name of the IoT hub this client will send requests to.</param>
        /// <param name="options">The optional settings for this client to use.</param>
        /// <returns>The created HTTP client.</returns>
        internal static HttpClient Create(string hostName, IotHubServiceClientOptions options)
        {
            Uri httpsEndpoint = new UriBuilder(HttpsEndpointPrefix, hostName).Uri;

            if (options.HttpClient != null)
            {
                Uri providedEndpoint = options.HttpClient.BaseAddress;
                return providedEndpoint.Equals(httpsEndpoint)
                    ? options.HttpClient
                    : throw new ArgumentException($"The provided HTTP client targets a different URI than expected. Expected: {httpsEndpoint}, Actual: {providedEndpoint}");
            }

// Http handlers created in this block are used within the returned HttpClient, so it cannot be disposed within this scope.
#pragma warning disable CA2000 // Dispose objects before losing scope.
#if NETCOREAPP
            var httpMessageHandler = new SocketsHttpHandler();
            httpMessageHandler.SslOptions.EnabledSslProtocols = options.SslProtocols;
            httpMessageHandler.SslOptions.RemoteCertificateValidationCallback = options.RemoteCertificateValidationCallback;
            if (!options.CertificateRevocationCheck)
            {
                httpMessageHandler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
            }
#else
            // This handler is used within the returned HttpClient, so it cannot be disposed within this scope.
            var httpMessageHandler = new HttpClientHandler
            {
                SslProtocols = options.SslProtocols,
                CheckCertificateRevocationList = options.CertificateRevocationCheck,
                ServerCertificateCustomValidationCallback = (httpRequest, certificate, chain, policyErrors) =>
                {
                    return options.RemoteCertificateValidationCallback.Invoke(httpRequest, certificate, chain, policyErrors);
                },
            };
#endif
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (options.Proxy != null)
            {
                httpMessageHandler.UseProxy = true;
                httpMessageHandler.Proxy = options.Proxy;
            }

            ServicePointHelpers.SetLimits(httpMessageHandler, httpsEndpoint);

            return new HttpClient(httpMessageHandler, true);
        }
    }
}
