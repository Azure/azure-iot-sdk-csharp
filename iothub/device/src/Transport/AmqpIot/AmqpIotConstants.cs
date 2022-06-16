// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal enum AmqpTwinMessageType
    {
        Get,
        Patch,
        Put,
    }

    internal static class AmqpIotConstants
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

        // The tracking Id identifier, for an Amqp Error returned by the service.
        public static readonly AmqpSymbol TrackingId = Vendor + ":tracking-id";

        // The client version identifier, added to Amqp link settings, while opening a sending link.
        // The client version is the product info of the device, for which the connection is being established.
        public static readonly AmqpSymbol ClientVersion = Vendor + ":client-version";

        // The API version identifier, added to Amqp link settings, while opening a sending link.
        // The API version identifies which version of the service API the connection is targeted towards.
        public static readonly AmqpSymbol ApiVersion = Vendor + ":api-version";

        // The correlation Id identifier, added to Amqp link settings, while opening a sending link.
        // The correlation Id is a guid prefixed with either "methods" or "twin", identifying the target of the sending link.
        public static readonly AmqpSymbol ChannelCorrelationId = Vendor + ":channel-correlation-id";

        // The authentication chain identifier, added to Amqp link settings, while opening a sending link.
        // The authentication chain is required for enabling nested Edge scenarios.
        public static readonly AmqpSymbol AuthChain = Vendor + ":auth-chain";

        // The digital twin model Id identifier, added to Amqp link settings, while opening a sending link.
        // The digital twin model Id is required to be sent over the sending link, in order for the service to identify it as a PnP-enabled device.
        public static readonly AmqpSymbol ModelId = Vendor + ":model-id";

        internal const string IotHubSasTokenType = CbsConstants.IotHubSasTokenType;
    }
}
