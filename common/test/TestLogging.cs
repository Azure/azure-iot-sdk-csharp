// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestLogging
    {
        private readonly static TestLogging s_instance = new TestLogging();

        private TestLogging()
        {
        }

        private const string NullInstance = "(null)";

        public static string IdOf(object value) => value != null
            ? $"{value.GetType().Name}#{GetHashCode(value)}"
            : NullInstance;

        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        public static TestLogging GetInstance()
        {
            return s_instance;
        }

        public void WriteLine(string message)
        {
            EventSourceTestLogging.Log.TestMessage(message);
        }
    }
}
