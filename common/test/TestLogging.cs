// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestLogging
    {
        private readonly static TestLogging s_instance = new TestLogging();

        private TestLogging()
        {
        }

        public static TestLogging GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            EventSourceTestLogging.Log.TestMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
