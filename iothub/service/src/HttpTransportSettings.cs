// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains Http1 transport-specific settings for Service Client
    /// </summary>
    public sealed class HttpTransportSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpTransportSettings"/> class.
        /// </summary>
        public HttpTransportSettings()
        {
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Proxy information.
        /// </summary>
        /// <remarks>
        /// This is used when a device is on a network that doesn't have direct internet access and needs to access it via a proxy,
        /// especially when MQTT and AMQP ports are disallowed to the internet.
        /// </remarks>
        public IWebProxy Proxy { get; set; }
    }
}
