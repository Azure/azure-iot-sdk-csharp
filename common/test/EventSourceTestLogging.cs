// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.E2ETests
{
    // f7ac322b-77f1-5a2d-0b56-ec79a41e82a2
    [EventSource(Name = "Microsoft-Azure-Devices-TestLogging")]
    internal class EventSourceTestLogging : EventSource
    {
        private EventSourceTestLogging()
        {
        }

        public static EventSourceTestLogging Log { get; } = new EventSourceTestLogging();

        [Event(1, Keywords = Keywords.Default, Level = EventLevel.Informational)]
        public void TestMessage(string message)
        {
            WriteEvent(1, Truncate(message));
        }

        [Event(2, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void TestVerboseMessage(string message)
        {
            WriteEvent(2, Truncate(message));
        }

        private string Truncate(string message)
        {
            // Max size is 64K, but includes all info, so limit message to less
            const int MaxEtwMessageSize = 60 * 1024;

            if (string.IsNullOrEmpty(message)) return message;
            return message.Length <= MaxEtwMessageSize
                ? message
                : message.Substring(0, MaxEtwMessageSize);
        }

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
