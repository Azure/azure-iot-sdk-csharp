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
    }
}
