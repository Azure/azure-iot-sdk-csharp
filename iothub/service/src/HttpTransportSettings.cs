// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// contains Http1 transport-specific settings for Service Client
    /// </summary>
    public sealed class HttpTransportSettings
    {
        public HttpTransportSettings()
        {
            this.Proxy = DefaultWebProxySettings.Instance;
        }

        public IWebProxy Proxy { get; set; }
    }
}