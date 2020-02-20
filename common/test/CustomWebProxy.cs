// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class CustomWebProxy : IWebProxy
    {
        private static readonly TestLogging s_testLog = TestLogging.GetInstance();
        private long _counter = 0;

        public ICredentials Credentials { get; set; }

        public long Counter => Interlocked.Read(ref _counter);

        public Uri GetProxy(Uri destination)
        {
#if NETCOREAPP1_1
            return destination; // otherwise causes NRE
#else
            return null;
#endif
        }

        public bool IsBypassed(Uri host)
        {
            Interlocked.Increment(ref _counter);
            s_testLog.WriteLine($"{nameof(CustomWebProxy)}.{nameof(IsBypassed)} Uri = {host}");
            return false;
        }
    }
}
