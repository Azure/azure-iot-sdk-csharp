// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal enum AmqpTwinMessageType
    {
        Get,
        Patch
    }

    internal static class AmqpIoTConstants
    {
        internal const string Vendor = "com.microsoft";

        internal const int DefaultSecurePort = AmqpConstants.DefaultSecurePort;
        internal const int ProtocolHeaderSize = AmqpConstants.ProtocolHeaderSize;
        internal const uint DefaultMaxFrameSize = AmqpConstants.DefaultMaxFrameSize;
        internal static readonly ArraySegment<byte> NullBinary = AmqpConstants.NullBinary;
        internal const uint AmqpBatchedMessageFormat = AmqpConstants.AmqpBatchedMessageFormat;
        internal static readonly Accepted AcceptedOutcome = AmqpConstants.AcceptedOutcome;

        internal const string ResponseStatusName = "status";
        internal const string TelemetrySenderLinkSuffix = "TelemetrySenderLink";
        internal const string TelemetryReceiveLinkSuffix = "TelemetryReceiverLink";
        internal const string EventsReceiverLinkSuffix = "EventsReceiverLink";
        internal const string MethodsSenderLinkSuffix = "MethodsSenderLink";
        internal const string MethodsReceiverLinkSuffix = "MethodsReceiverLink";
        internal const string MethodCorrelationIdPrefix = "methods:";
        internal const string TwinSenderLinkSuffix = "TwinSenderLink";
        internal const string TwinReceiverLinkSuffix = "TwinReceiverLink";
        internal const string TwinCorrelationIdPrefix = "twin:";

        internal const string MethodName = "IoThub-methodname";
        internal const string Status = "IoThub-status";

        internal const string IotHubSasTokenType = CbsConstants.IotHubSasTokenType;
    }
}
