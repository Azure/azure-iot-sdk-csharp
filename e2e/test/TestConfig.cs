// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.Tracing;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    public static class TestConfig
    {
        private static readonly ConsoleEventListener _listener = new ConsoleEventListener("Microsoft-Azure-");

        public static ConsoleEventListener StartEventListener()
        {
            return _listener;
        }
    }
}
