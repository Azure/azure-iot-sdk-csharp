// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The transport settings configurable on a service client instance.
    /// </summary>
    public sealed class ServiceClientTransportSettings
    {
        /// <summary>
        /// Creates an instance of <see cref="ServiceClientTransportSettings"/> with the default proxy settings.
        /// </summary>
        public ServiceClientTransportSettings()
        {
            AmqpProxy = DefaultWebProxySettings.Instance;
            HttpProxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// The web proxy that will be used to connect to IoT Hub when using the AMQP over web sockets.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// This setting will be used when the client attempts to connect over web sockets. For example, if the client attempts to connect to IoT hub using <see cref="TransportType.Amqp"/> the client will first try over TCP. If that fails, the client will fall back to using web sockets and will use the proxy setting. This setting is to be used in conjunction with the <see cref="HttpProxy"/> property.
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <code>
        /// static ServiceClient GetServiceClient()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         var proxy = new WebProxy(proxyHost, proxyPort);
        ///         var transportSettings = new ServiceClientTransportSettings()
        ///         {
        ///             AmqpProxy = proxy,
        ///             HttpProxy = proxy
        ///         };
        ///         var serviceClient = ServiceClient.CreateFromConnectionString("a connection string", Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only, transportSettings );
        ///         return serviceClient;
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </code>
        /// </example>
        public IWebProxy AmqpProxy { get; set; }

        /// <summary>
        /// The web proxy that will be used to connect to IoT Hub when operations must execute over HTTP.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// Methods such as <see cref="ServiceClient.GetServiceStatisticsAsync(System.Threading.CancellationToken)"/> are executed over HTTP and not AMQP. This setting will ensure those methods are executed over the specified proxy. This setting is to be used in conjunction with the <see cref="AmqpProxy"/> property. This setting is only valid if <see cref="TransportType.Amqp_WebSocket_Only"/> is set. Or, if <see cref="TransportFallbackType"/>
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <code>
        /// static ServiceClient GetServiceClient()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         var proxy = new WebProxy(proxyHost, proxyPort);
        ///         var transportSettings = new ServiceClientTransportSettings()
        ///         {
        ///             AmqpProxy = proxy,
        ///             HttpProxy = proxy
        ///         };
        ///         var serviceClient = ServiceClient.CreateFromConnectionString("a connection string", Microsoft.Azure.Devices.TransportType.Amqp_WebSocket_Only, transportSettings );
        ///         return serviceClient;
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </code>
        /// </example>
        public IWebProxy HttpProxy { get; set; }

        /// <summary>
        /// How long, in milliseconds, a given cached TCP connection created by this client's HTTP layer will live before being closed. 
        /// If this value is set to any negative value, the connection lease will be infinite. If this value is set to 0, then the TCP connection will close after
        /// each HTTP request and a new TCP connection will be opened upon the next request.
        /// </summary>
        /// <remarks>
        /// By closing cached TCP connections and opening a new one upon the next request, the underlying HTTP client has a chance to do a DNS lookup 
        /// to validate that it will send the requests to the correct IP address. While it is atypical for a given IoT Hub to change its IP address, it does
        /// happen when a given IoT Hub fails over into a different region. Because of that, users who expect to failover their IoT Hub at any point
        /// are advised to set this value to a value of 0 or greater. Larger values will make better use of caching to save network resources over time,
        /// but smaller values will make the client respond more quickly to failed over IoT Hubs.
        /// </remarks>
        public int ConnectionLeaseTimeoutMilliseconds { get; set; } = ServicePointHelpers.DefaultConnectionLeaseTimeout;
    }
}
