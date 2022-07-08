﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The configurable options for <see cref="ServiceClient2"/> instances.
    /// </summary>
    public class ServiceClientOptions2
    {
        /// <summary>
        /// Initializes a new instance of this class using the default settings.
        /// </summary>
        /// <param name="version">
        /// The service API version that this client will use when making service requests. Defaults to the latest
        /// version.
        /// </param>
        public ServiceClientOptions2(ServiceVersion version = LatestVersion)
        {
            Proxy = DefaultWebProxySettings.Instance;
            Version = version;
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
        /// static ServiceClient GetServiceClient()
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
        ///         var serviceClient = new ServiceClient("a connection string", transportSettings);
        ///         return serviceClient;
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
        public TimeSpan HttpConnectionLeaseTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The HTTP client to use for all HTTP operations. If provided, all other settings will be ignored. If not provided,
        /// an HTTP client will be created for you based on the other provided settings.
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Gets the <see cref="ServiceVersion"/> of the service API used when
        /// making requests.
        /// </summary>
        public ServiceVersion Version { get; set; } = LatestVersion;

        /// <summary>
        /// The service API versions that the service supports.
        /// </summary>
        public enum ServiceVersion
        {
            /// <summary>
            /// 2021-04-12
            /// </summary>
            V2021_04_12 = 1,

            /// <summary>
            /// 2020-03-13
            /// </summary>
            V2020_03_13 = 2,

            /// <summary>
            /// 2019-10-01
            /// </summary>
            V2019_10_01 = 3,

            /// <summary>
            /// 2020-09-30
            /// </summary>
            V2019_09_30 = 4,

            /// <summary>
            /// 2019-03-30
            /// </summary>
            V2019_03_30 = 5,

            /// <summary>
            /// 2018-06-30
            /// </summary>
            V2018_06_30 = 6,

            /// <summary>
            /// 2018_04_01
            /// </summary>
            V2018_04_01 = 7
        }

        internal const ServiceVersion LatestVersion = ServiceVersion.V2021_04_12;

        internal string GetVersionString()
        {
            return Version switch
            {
                ServiceVersion.V2021_04_12 => "2021-04-12",
                ServiceVersion.V2020_03_13 => "2020-03-13",
                ServiceVersion.V2019_10_01 => "2019-10-01",
                ServiceVersion.V2019_09_30 => "2020-09-30",
                ServiceVersion.V2019_03_30 => "2019-03-30",
                ServiceVersion.V2018_06_30 => "2018-06-30",
                ServiceVersion.V2018_04_01 => "2018-04-01",
                _ => throw new ArgumentException(Version.ToString()),
            };
        }
    }
}
