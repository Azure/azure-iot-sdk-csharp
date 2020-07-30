// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class CustomWebProxy : IWebProxy
    {
        private readonly TestLogger s_testLog;
        private long _counter = 0;

        public CustomWebProxy(TestLogger logger)
        {
            s_testLog = logger;
        }

        public ICredentials Credentials { get; set; }

        public long Counter => Interlocked.Read(ref _counter);

        public Uri GetProxy(Uri destination)
        {
            return null;
        }

        public bool IsBypassed(Uri host)
        {
            Interlocked.Increment(ref _counter);
            s_testLog.Trace($"{nameof(CustomWebProxy)}.{nameof(IsBypassed)} Uri = {host}");
            return false;
        }
    }
}
