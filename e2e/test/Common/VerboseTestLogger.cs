// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class VerboseTestLogger
    {
        private static readonly VerboseTestLogger s_instance = new VerboseTestLogger();

        private VerboseTestLogger()
        {
        }

        public static VerboseTestLogger GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogger.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, args);
            EventSourceTestLogger.Log.TestVerboseMessage(message);
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
