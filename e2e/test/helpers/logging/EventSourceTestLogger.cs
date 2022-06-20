// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.E2ETests
{
    // f7ac322b-77f1-5a2d-0b56-ec79a41e82a2
    [EventSource(Name = "Microsoft-Azure-Devices-TestLogging")]
    internal class EventSourceTestLogger : EventSource
    {
        private static EventSourceTestLogger s_log = new EventSourceTestLogger();

        private EventSourceTestLogger()
        {
        }

        public static EventSourceTestLogger Log => s_log;

        [Event(1, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void TestMessage(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void TestVerboseMessage(string message)
        {
            WriteEvent(2, message);
        }

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
