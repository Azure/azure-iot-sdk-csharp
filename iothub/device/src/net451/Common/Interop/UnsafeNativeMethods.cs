// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace Microsoft.Azure.Devices.Client
{
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        public const string KERNEL32 = "kernel32.dll";
        public const string ADVAPI32 = "advapi32.dll";

        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INVALID_HANDLE = 6;
        public const int ERROR_OUTOFMEMORY = 14;
        public const int ERROR_MORE_DATA = 234;
        public const int ERROR_ARITHMETIC_OVERFLOW = 534;
        public const int ERROR_NOT_ENOUGH_MEMORY = 8;
        public const int ERROR_OPERATION_ABORTED = 995;
        public const int ERROR_IO_PENDING = 997;
        public const int ERROR_NO_SYSTEM_RESOURCES = 1450;

        public const int STATUS_PENDING = 0x103;

        // socket errors
        public const int WSAACCESS = 10013;
        public const int WSAEMFILE = 10024;
        public const int WSAEMSGSIZE = 10040;
        public const int WSAEADDRINUSE = 10048;
        public const int WSAEADDRNOTAVAIL = 10049;
        public const int WSAENETDOWN = 10050;
        public const int WSAENETUNREACH = 10051;
        public const int WSAENETRESET = 10052;
        public const int WSAECONNABORTED = 10053;
        public const int WSAECONNRESET = 10054;
        public const int WSAENOBUFS = 10055;
        public const int WSAESHUTDOWN = 10058;
        public const int WSAETIMEDOUT = 10060;
        public const int WSAECONNREFUSED = 10061;
        public const int WSAEHOSTDOWN = 10064;
        public const int WSAEHOSTUNREACH = 10065;

        [DllImport(KERNEL32)]
        [ResourceExposure(ResourceScope.None)]
        [SecurityCritical]
        internal static extern bool IsDebuggerPresent();

        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [ResourceExposure(ResourceScope.Machine)]
        [SecurityCritical]
        internal static extern SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName);
    }
}
