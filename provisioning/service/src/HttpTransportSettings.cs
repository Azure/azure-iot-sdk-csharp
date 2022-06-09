// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// contains Http1 transport-specific settings for Service Client
    /// </summary>
    public sealed class HttpTransportSettings
    {
        /// <summary>
        /// Creates an instance of Http1TransportSettings
        /// </summary>
        public HttpTransportSettings()
        {
            Proxy = DefaultWebProxySettings.Instance;
        }

        /// <summary>
        /// Gets or sets proxy information for the request.
        /// </summary>
        public IWebProxy Proxy { get; set; }
    }
}
