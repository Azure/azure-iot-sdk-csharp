// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Microsoft.Azure.Devices.Samples
{
    internal class ExceptionHelper
    {
        private static readonly HashSet<Type> s_networkExceptions = new HashSet<Type>
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(TimeoutException),
            typeof(OperationCanceledException),
            typeof(WebException),
            typeof(WebSocketException),
        };

        internal static bool IsNetwork(Exception singleException)
        {
            return s_networkExceptions.Any(baseExceptionType => baseExceptionType.IsInstanceOfType(singleException));
        }
    }
}
