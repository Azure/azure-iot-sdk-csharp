// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// Connection keys supported by DeviceClientConnectionStatusManager
    /// </summary>
    public static class ConnectionKeys
    {
        public static string AmqpTelemetry = "AmqpTelemetry";
        public static string AmqpMessaging = "AmqpMessaging";
        public static string AmqpMethodSending = "AmqpMethodSending";
        public static string AmqpMethodReceiving = "AmqpMethodReceiving";
        public static string AmqpTwinSending = "AmqpTwinSending";
        public static string AmqpTwinReceiving = "AmqpTwinReceiving";

        public static string MqttConnection = "MqttConnection";
    }
}
