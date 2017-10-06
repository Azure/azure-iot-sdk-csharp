// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public class HttpTransportClient : TransportClient
    {
        public HttpTransportClient() : base()
        {

        }

        public HttpTransportClient(TransportFallbackType transportFallback)
        {
            throw new NotSupportedException();
        }

        public override void Dispose()
        {
            throw new NotSupportedException();
        }
    }
}
