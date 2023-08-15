// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Discovery.Client.Transport.Http;
using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    internal abstract class HttpAuthStrategy
    {
        public static readonly TimeSpan TimeoutConstant = TimeSpan.FromSeconds(90);

        public abstract EdgeDiscoveryService CreateClient(Uri uri, HttpClientHandler httpClientHandler);
    }
}
