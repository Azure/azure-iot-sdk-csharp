// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal static class MessageSystemPropertyNames
    {
        public const string MessageId = "message-id";

        public const string LockToken = "iothub-messagelocktoken";

        public const string SequenceNumber = "sequence-number";

        public const string To = "to";

        public const string EnqueuedTime = "iothub-enqueuedtime";

        public const string ExpiryTimeUtc = "absolute-expiry-time";

        public const string CorrelationId = "correlation-id";

        public const string DeliveryCount = "iothub-deliverycount";

        public const string UserId = "user-id";

        public const string Operation = "iothub-operation";

        public const string Ack = "iothub-ack";

        public const string OutputName = "iothub-outputname";

        public const string InputName = "iothub-inputname";

        public const string MessageSchema = "iothub-message-schema";

        public const string CreationTimeUtc = "iothub-creation-time-utc";

        public const string ContentEncoding = "iothub-content-encoding";

        public const string ContentType = "iothub-content-type";

        public const string ConnectionDeviceId = "iothub-connection-device-id";

        public const string ConnectionModuleId = "iothub-connection-module-id";

        public const string DiagId = "iothub-diag-id";

        public const string DiagCorrelationContext = "diag-correlation-context";

        public const string InterfaceId = "iothub-interface-id";

        public const string ComponentName = "dt-subject";
    }
}
