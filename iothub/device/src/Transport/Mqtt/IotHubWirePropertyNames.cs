// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// MQTT constants for serialized IoT hub properties.
    /// </summary>
    internal static class IotHubWirePropertyNames
    {
        internal const string AbsoluteExpiryTime = "$.exp";
        internal const string CorrelationId = "$.cid";
        internal const string MessageId = "$.mid";
        internal const string To = "$.to";
        internal const string UserId = "$.uid";
        internal const string OutputName = "$.on";
        internal const string MessageSchema = "$.schema";
        internal const string CreationTimeUtc = "$.ctime";
        internal const string ContentType = "$.ct";
        internal const string ContentEncoding = "$.ce";
        internal const string ConnectionDeviceId = "$.cdid";
        internal const string ConnectionModuleId = "$.cmid";
        internal const string MqttDiagIdKey = "$.diagid";
        internal const string MqttDiagCorrelationContextKey = "$.diagctx";
        internal const string InterfaceId = "$.ifid";
        internal const string ComponentName = "$.sub";
    }
}
