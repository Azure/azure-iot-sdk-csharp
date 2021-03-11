// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace System.Diagnostics.Tracing
{
    public sealed class ConsoleEventListener : EventListener
    {
        // Configure this value to filter all the necessary events when OnEventSourceCreated is called.
        // OnEventSourceCreated is triggered as soon as the EventListener is registered and an event source is created.
        // So trying to configure this value in the ConsoleEventListener constructor does not work.
        // The OnEventSourceCreated can be triggered sooner than the filter is initialized in the ConsoleEventListener constructor.
        private static readonly string[] s_eventFilters = new string[] { "DotNetty-Default", "Microsoft-Azure-Devices", "Azure-Core", "Azure-Identity" };

        private readonly object _lock = new object();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (s_eventFilters.Any(filter => eventSource.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)))
            {
                base.OnEventSourceCreated(eventSource);
                EnableEvents(
                    eventSource,
                    EventLevel.LogAlways
#if !NET451
                , EventKeywords.All
#endif
                );
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (_lock)
            {
                string eventIdent;
#if NET451
                    // net451 doesn't have EventName, so we'll settle for EventId
                    eventIdent = eventData.EventId.ToString(CultureInfo.InvariantCulture);
#else
                eventIdent = eventData.EventName;
#endif
                string text = $"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)} [{eventData.EventSource.Name}-{eventIdent}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";

                ConsoleColor origForeground = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(text);
                Debug.WriteLine(text);
                Console.ForegroundColor = origForeground;
            }
        }
    }
}
