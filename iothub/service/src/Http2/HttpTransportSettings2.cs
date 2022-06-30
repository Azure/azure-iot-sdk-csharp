﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains HTTP transport-specific settings for these clients.
    /// </summary>
    public sealed class HttpTransportSettings2
    {
        /// <summary>
        /// Initializes a new instance of this class using the default settings.
        /// </summary>
        public HttpTransportSettings2()
        {
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// The web proxy that will be used to connect to IoT hub when using the HTTP protocol.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <c>
        /// static RegistryClient GetRegistryClient()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         var transportSettings = new HttpTransportSettings
        ///         {
        ///             Proxy = new WebProxy(proxyHost, proxyPort)
        ///         };
        ///         // Specify the WebProxy to be used for the HTTP connection
        ///         var registryClient = new RegistryClient("a connection string", transportSettings);
        ///         return registryClient;
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </c>
        /// </example>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// How long, in milliseconds, a given cached TCP connection created by this client's HTTP layer will live before being closed.
        /// If this value is set to any negative value, the connection lease will be infinite. If this value is set to 0, then the TCP connection will close after
        /// each HTTP request and a new TCP connection will be opened upon the next request.
        /// </summary>
        /// <remarks>
        /// By closing cached TCP connections and opening a new one upon the next request, the underlying HTTP client has a chance to do a DNS lookup
        /// to validate that it will send the requests to the correct IP address. While it is atypical for a given IoT hub to change its IP address, it does
        /// happen when a given IoT hub fails over into a different region. Because of that, users who expect to failover their IoT hub at any point
        /// are advised to set this value to a value of 0 or greater. Larger values will make better use of caching to save network resources over time,
        /// but smaller values will make the client respond more quickly to failed over IoT hubs.
        /// </remarks>
        public int ConnectionLeaseTimeoutMilliseconds { get; set; } = ServicePointHelpers.DefaultConnectionLeaseTimeout;

        /// <summary>
        /// The HTTP client to use for all HTTP operations. If provided, all other settings will be ignored. If not provided,
        /// an HTTP client will be created for you based on the other provided settings.
        /// </summary>
        public HttpClient HttpClient { get; set; }
    }
}
