// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Connection types supported by DeviceClientConnectionStatusManager
    /// </summary>
    public enum ConnectionType
    {
        AmqpTelemetry,
        AmqpMessaging,
        AmqpMethodSending,
        AmqpMethodReceiving,
        AmqpTwinSending,
        AmqpTwinReceiving,

        MqttConnection,
    }
}
