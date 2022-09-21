// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    /// <summary>
    /// MQTT constants for serialized IoT hub properties.
    /// </summary>
    internal static class IotHubWirePropertyNames
    {
        public const string AbsoluteExpiryTime = "$.exp";
        public const string CorrelationId = "$.cid";
        public const string MessageId = "$.mid";
        public const string To = "$.to";
        public const string UserId = "$.uid";
        public const string OutputName = "$.on";
        public const string MessageSchema = "$.schema";
        public const string CreationTimeUtc = "$.ctime";
        public const string ContentType = "$.ct";
        public const string ContentEncoding = "$.ce";
        public const string ConnectionDeviceId = "$.cdid";
        public const string ConnectionModuleId = "$.cmid";
        public const string MqttDiagIdKey = "$.diagid";
        public const string MqttDiagCorrelationContextKey = "$.diagctx";
        public const string InterfaceId = "$.ifid";
        public const string ComponentName = "$.sub";
    }
}
