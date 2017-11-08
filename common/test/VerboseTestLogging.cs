// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class VerboseTestLogging
    {
        private static readonly VerboseTestLogging s_instance = new VerboseTestLogging();

        private VerboseTestLogging()
        {
        }

        public static VerboseTestLogging GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            EventSourceTestLogging.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
