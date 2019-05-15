// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal static class AmqpIoTConstants
    {
        internal const string Vendor = "com.microsoft";

        internal const int DefaultSecurePort = AmqpConstants.DefaultSecurePort;
        internal const int ProtocolHeaderSize = AmqpConstants.ProtocolHeaderSize;
        internal const uint DefaultMaxFrameSize = AmqpConstants.DefaultMaxFrameSize;
        internal static readonly ArraySegment<byte> NullBinary = AmqpConstants.NullBinary;
        internal const uint AmqpBatchedMessageFormat = AmqpConstants.AmqpBatchedMessageFormat;
    }
}
