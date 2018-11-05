// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client
{
    internal static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
               int dwOSMajorVersion,
               int dwOSMinorVersion,
               int dwSpMajorVersion,
               int dwSpMinorVersion,
               out int pdwReturnedProductType
           );

        public static int GetWindowsProductType()
        {
#if !NETSTANDARD1_3
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out int productType))
                {
                    return productType;
                }
            }
            catch (DllNotFoundException)
            {
                // Catch any DLL not found exceptions
            }
#endif
            return 0;
        }
    }
}
