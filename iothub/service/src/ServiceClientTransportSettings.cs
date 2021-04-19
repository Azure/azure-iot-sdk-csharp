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
        /// The proxy settings to be used on the AMQP client.
        /// </summary>
        public IWebProxy AmqpProxy { get; set; }

        /// <summary>
        /// The proxy settings to be used on the HTTP client.
        /// </summary>
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
