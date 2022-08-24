// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Factory for creating HTTP clients for the various service clients to use when making
    /// HTTP requests to the service. Each service client should only need to create one HTTP client
    /// for its lifetime.
    /// </summary>
    internal class HttpClientFactory
    {
        private const string HttpsEndpointPrefix = "https";

        // These default values are consistent with Azure.Core default values:
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L28
        private const int DefaultMaxConnectionsPerServer = 50;

        // How long, in milliseconds, a given cached TCP connection created by this client's HTTP layer will live before being closed.
        // If this value is set to any negative value, the connection lease will be infinite. If this value is set to 0, then the TCP connection will close after
        // each HTTP request and a new TCP connection will be opened upon the next request.
        //
        // By closing cached TCP connections and opening a new one upon the next request, the underlying HTTP client has a chance to do a DNS lookup
        // to validate that it will send the requests to the correct IP address. While it is atypical for a given IoT hub to change its IP address, it does
        // happen when a given IoT hub fails over into a different region.
        //
        // This default value is consistent with the default value used in Azure.Core
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L29
        private static readonly TimeSpan DefaultConnectionLeaseTimeout = TimeSpan.FromMinutes(5);

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
                if (!providedEndpoint.Equals(httpsEndpoint))
                {
                    throw new ArgumentException($"The provided HTTP client targets a different URI than expected. Expected: {httpsEndpoint}, Actual: {providedEndpoint}");
                }

                return options.HttpClient;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope.
            // This handler is used within the returned HttpClient, so it cannot be disposed within this scope.
            var httpMessageHandler = new HttpClientHandler();
#pragma warning restore CA2000 // Dispose objects before losing scope
            httpMessageHandler.SslProtocols = options.SslProtocols;
            httpMessageHandler.CheckCertificateRevocationList = options.CertificateRevocationCheck;

            if (options.Proxy != DefaultWebProxySettings.Instance)
            {
                httpMessageHandler.UseProxy = options.Proxy != null;
                httpMessageHandler.Proxy = options.Proxy;
            }

            httpMessageHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
            ServicePoint servicePoint = ServicePointManager.FindServicePoint(httpsEndpoint);
            servicePoint.ConnectionLeaseTimeout = DefaultConnectionLeaseTimeout.Milliseconds;

            return new HttpClient(httpMessageHandler);
        }
    }
}
