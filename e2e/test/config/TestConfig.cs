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
            new[]
            {
                "DotNetty-Default",
                "Microsoft-Azure-",
            });

        public static ConsoleEventListener StartEventListener()
        {
            return _listener;
        }
    }
}
