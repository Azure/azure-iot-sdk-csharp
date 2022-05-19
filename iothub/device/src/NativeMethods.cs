// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Azure.Devices.Shared;

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
               out int pdwReturnedProductType);

        public static int GetWindowsProductType()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && GetProductInfo(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, 0, 0, out int productType))
                {
                    return productType;
                }
            }
            catch (DllNotFoundException ex)
            {
                // Catch any DLL not found exceptions
                Debug.Assert(false, ex.Message);
                
                if (Logging.IsEnabled)
                    Logging.Error(null, ex, nameof(NativeMethods));
            }

            return 0;
        }
    }
}
