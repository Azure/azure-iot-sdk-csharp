// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The configurable options for <see cref="IotHubServiceClient"/> instances.
    /// </summary>
    public class IotHubServiceClientOptions
    {
        /// <summary>
        /// Initializes a new instance of this class using the default settings.
        /// </summary>
        public IotHubServiceClientOptions()
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
        /// The HTTP client to use for all HTTP operations. If provided, all other settings will be ignored. If not provided,
        /// an HTTP client will be created for you based on the other provided settings.
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// To enable certificate revocation check.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool CertificateRevocationCheck { get; set; }
    }
}
