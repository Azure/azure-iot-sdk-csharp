// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.Tracing;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public static class TestConfig
    {
        private static readonly ConsoleEventListener _listener = new ConsoleEventListener(new string[]
            {
            //"DotNetty-Default", // Disabling SDK listeners as they are incompatible with parallel test execution.
            //"Microsoft-Azure-",
            "Microsoft-Azure-Devices-TestLogging",
            });

        public static ConsoleEventListener StartEventListener()
        {
            return _listener;
        }
    }
}
