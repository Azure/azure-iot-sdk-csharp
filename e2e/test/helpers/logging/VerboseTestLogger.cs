// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class VerboseTestLogger
    {
        public static void WriteLine(string message)
        {
            EventSourceTestLogger.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
