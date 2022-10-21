// Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client.Samples
{
    // Sample exception handler class - this class should be modified based on your application's logic.
    internal class ExceptionHelper
    {
        private static readonly HashSet<Type> s_networkExceptions = new()
        {
            typeof(IOException),
            typeof(SocketException),
            typeof(ClosedChannelException),
            typeof(TimeoutException),
            typeof(OperationCanceledException),
            typeof(HttpRequestException),
            typeof(WebException),
            typeof(WebSocketException),
        };
        private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        internal static bool IsNetworkExceptionChain(Exception exceptionChain)
        {
            return Unwind(exceptionChain, true).Any(e => IsNetwork(e) && !IsTlsSecurity(e));
        }

        private static bool IsNetwork(Exception singleException)
        {
            return s_networkExceptions.Any(baseExceptionType => baseExceptionType.IsInstanceOfType(singleException));
        }

        private static bool IsTlsSecurity(Exception singleException)
        {
            if (singleException is AuthenticationException)
            {
                return true;
            }

            if (s_isWindows)
            {
                // WinHttpException (0x80072F8F): A security error occurred.
                if (singleException.HResult == unchecked((int)0x80072F8F))
                {
                    return true;
                }
            }
            else // Linux
            {
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                if (singleException.HResult == 60)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Exception> Unwind(Exception exception, bool unwindAggregate = false)
        {
            while (exception != null)
            {
                yield return exception;

                if (!unwindAggregate)
                {
                    exception = exception.InnerException;
                    continue;
                }

                if (exception is AggregateException aggEx
                    && aggEx.InnerExceptions != null)
                {
                    foreach (Exception ex in aggEx.InnerExceptions)
                    {
                        foreach (Exception innerEx in Unwind(ex, true))
                        {
                            yield return innerEx;
                        }
                    }
                }

                exception = exception.InnerException;
            }
        }
    }
}
