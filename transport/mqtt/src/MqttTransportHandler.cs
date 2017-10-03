// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport
{
    public class MqttTransportClient : TransportClient
    {
        public MqttTransportClient() : base()
        {

        }

        public MqttTransportClient(TransportFallbackType transportFallback) : base(transportFallback)
        {
        }

        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
