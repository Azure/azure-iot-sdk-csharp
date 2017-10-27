// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal static class WebSocketConstants
    {
        public const string Scheme = "wss";
        public const string Version = "13";
        public const int Port = 443;

        public const int BufferSize = 8 * 1024;

        public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(15);

        internal static class SubProtocols
        {
            public const string Amqpwsb10 = "AMQPWSB10";
        }
    }
}
