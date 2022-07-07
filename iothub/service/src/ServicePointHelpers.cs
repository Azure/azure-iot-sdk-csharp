// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Devices
{
    // This type manages changing HttpClient defaults to more appropriate values
    // There are two limits we target:
    // - Per Server Connection Limit
    // - Keep Alive Connection Timeout
    // On .NET Core 2.1+ the HttpClient defaults to using the HttpSocketHandler so we adjust both limits on the client handler
    //
    // On .NET Standard & NET 4.51+ the HttpClient defaults to using the HttpClientHandler
    //   and there is no easy way to set Keep Alive Connection Timeout but it's mitigated by setting the service point's connection lease timeout
    internal static class ServicePointHelpers
    {
        // These default values are consistent with Azure.Core default values:
        // https://github.com/Azure/azure-sdk-for-net/blob/7e3cf643977591e9041f4c628fd4d28237398e0b/sdk/core/Azure.Core/src/Pipeline/ServicePointHelpers.cs#L28
        internal const int DefaultMaxConnectionsPerServer = 50;

        internal const int DefaultConnectionLeaseTimeout = 300 * 1000; // 5 minutes

        // messageHandler passed in is an HttpClientHandler for .NET Framework and .NET standard, and a SocketsHttpHandler for .NET core
        public static void SetLimits(HttpMessageHandler messageHandler, Uri baseUri, int connectionLeaseTimeoutMilliseconds)
        {
            switch (messageHandler)
            {
                case HttpClientHandler httpClientHandler:
                    httpClientHandler.MaxConnectionsPerServer = DefaultMaxConnectionsPerServer;
                    ServicePoint servicePoint = ServicePointManager.FindServicePoint(baseUri);
                    servicePoint.ConnectionLeaseTimeout = connectionLeaseTimeoutMilliseconds;
                    break;
            }
        }
    }
}
