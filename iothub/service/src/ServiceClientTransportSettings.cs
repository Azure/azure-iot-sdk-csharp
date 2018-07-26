// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// contains the AMQP and HTTP proxy settings for Service Client
    /// </summary>
    public sealed class ServiceClientTransportSettings
    {
        public ServiceClientTransportSettings()
        {
            AmqpProxy = DefaultWebProxySettings.Instance;
            HttpProxy = DefaultWebProxySettings.Instance;
        }

        public IWebProxy AmqpProxy { get; set; }

        public IWebProxy HttpProxy { get; set; }
    }
}
