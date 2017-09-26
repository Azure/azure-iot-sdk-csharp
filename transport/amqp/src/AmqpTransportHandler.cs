// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public class AmqpTransportHandler : TransportHandler
    {
        public AmqpTransportHandler() : base()
        {

        }

        public AmqpTransportHandler(TransportFallbackType transportFallback) : base(transportFallback)
        {
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
