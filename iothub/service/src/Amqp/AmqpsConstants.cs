// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;

namespace Microsoft.Azure.Devices.Amqp
{
    internal class AmqpsConstants
    {
        public const string FileUploadNotificationsAddress = "/messages/serviceBound/filenotifications";
        public const string FeedbackMessageAddress = "/messages/servicebound/feedback";
        public const string CloudToDeviceMessageAddress = "/messages/devicebound";
        public const string Amqpwsb10 = "AMQPWSB10";
        public const string Scheme = "wss://";
        public const string UriSuffix = "/$iothub/websocket";
        public const string SecurePort = "443";
        public const string Version = "13";

        public static readonly AmqpSymbol TimeoutName = AmqpConstants.Vendor + ":timeout";
        public static readonly AmqpSymbol StackTraceName = AmqpConstants.Vendor + ":stack-trace";
        public static readonly AmqpSymbol TrackingId = AmqpConstants.Vendor + ":tracking-id";
        public static readonly AmqpSymbol ErrorCode = AmqpConstants.Vendor + ":error-code";
        public static readonly AmqpSymbol ClientVersion = AmqpConstants.Vendor + ":client-version";

        public const string BatchedFeedbackContentType = "application/vnd.microsoft.iothub.feedback.json";
        public const string FileNotificationContentType = "application/vnd.microsoft.iothub.filenotification.json";
    }
}
