// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;

    /// <summary>
    /// Define enum for all types of links used by IoTHub Client SDK
    /// </summary>
    internal enum AmqpClientLinkType
    {
        /// <summary>
        /// Link for sending telemetry data from device to IoTHub
        /// </summary>
        TelemetrySender,
        /// <summary>
        /// Link for receiving messages from IoTHub (Cloud to Device messages)
        /// </summary>
        C2D,
        /// <summary>
        /// Link for sending responses of Method called bu IoTHub
        /// </summary>
        MethodsSender,
        /// <summary>
        /// Link for receiving Method calls from IoTHub
        /// </summary>
        MethodsReceiver,
        /// <summary>
        /// Link for sending Twin data to IoTHub
        /// </summary>
        TwinSender,
        /// <summary>
        /// Link for receive Twin data (and pathes) from IoTHub
        /// </summary>
        TwinReceiver,
        /// <summary>
        /// Link for receiving events from EventHub
        /// </summary>
        EventsReceiver
    }

    /// <summary>
    /// Factory interface to create AmqpClientLink objects for Amqp transport layer
    /// </summary>
    internal interface IAmqpClientLinkFactory
    {
        AmqpClientLink Create(AmqpClientLinkType amqpClientLinkType, AmqpClientSession amqpClientSession, DeviceClientEndpointIdentity deviceClientEndpointIdentity, TimeSpan timeout, string correlationid = "");
    }
}