// Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;

namespace Microsoft.Azure.IoT.Thief.Device
{
    // Sample exception handler class - this class should be modified based on your application's logic
    internal class ExceptionHelper
    {
        internal static bool IsSecurityExceptionChain(Exception exceptionChain)
        {
            return exceptionChain.Unwind(true).Any(e => IsTlsSecurity(e));
        }

        private static bool IsTlsSecurity(Exception singleException)
        {
            if (// WinHttpException (0x80072F8F): A security error occurred.
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (singleException.HResult == unchecked((int)0x80072F8F))) ||
                // CURLE_SSL_CACERT (60): Peer certificate cannot be authenticated with known CA certificates.
                (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (singleException.HResult == 60)) ||
                singleException is AuthenticationException)
            {
                return true;
            }

            return false;
        }
    }
}
