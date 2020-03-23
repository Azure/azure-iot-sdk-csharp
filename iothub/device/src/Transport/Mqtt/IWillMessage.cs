// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Codecs.Mqtt.Packets;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    public interface IWillMessage
    {
        Message Message { get; }

        QualityOfService QoS { get; set; }
    }
}
