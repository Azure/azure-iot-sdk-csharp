// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class CustomWebProxy : IWebProxy
    {
        private static TestLogging s_testLog = TestLogging.GetInstance();
        private long _counter = 0;

        public ICredentials Credentials { get; set; }

        public long Counter {
            get
            {
                return Interlocked.Read(ref _counter);
            }
        }

        public Uri GetProxy(Uri destination)
        {
            return null;
        }

        public bool IsBypassed(Uri host)
        {
            Interlocked.Increment(ref _counter);
            s_testLog.WriteLine($"{nameof(CustomWebProxy)}.{nameof(IsBypassed)} Uri = {host}");
            return false;
        }
    }
}
