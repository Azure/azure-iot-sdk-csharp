// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//Workers = 0 makes the test engine use one worker per available core. It does not mean to run serially.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class TestConfig
    {
        private static readonly ConsoleEventListener _listener = new ConsoleEventListener(
            new string[]
            {
#if !NETCOREAPP1_1 // avoid ETW logging bugs from a library we can't control
                "DotNetty-Default",
#endif
                "Microsoft-Azure-",
            });

        public static ConsoleEventListener StartEventListener()
        {
            return _listener;
        }
    }
}
