// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq;

namespace System.Diagnostics.Tracing
{
    public sealed class ConsoleEventListener : EventListener
    {
        // Configure this value to filter all the necessary events when OnEventSourceCreated is called.
        // The EventListener base class constructor creates an event listener in which all events are disabled by default.
        // EventListener constructor also causes the OnEventSourceCreated callback to fire.
        // Since our ConsoleEventListener uses the OnEventSourceCreated callback to enable events, the event filter needs to be
        // initialized before OnEventSourceCreated is called. For this reason we cannot use ConsoleEventListener constructor
        // to initialize the event filter (base class constructors are called before derived class constructors).
        // The OnEventSourceCreated will be triggered sooner than the filter is initialized in the ConsoleEventListener constructor.
        // As a result we will need to define the event filter list as a static variable.
        // Link to EventListener sourcecode: https://github.com/dotnet/runtime/blob/6696065ab0f517f5a9e5f55c559df0010a816dbe/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/EventSource.cs#L4009-L4018
        private static readonly string[] s_eventFilter = new string[] { "DotNetty-Default", "Microsoft-Azure-Devices", "Azure-Core", "Azure-Identity" };

        private readonly object _consoleLock = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (s_eventFilter.Any(filter => eventSource.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)))
            {
                base.OnEventSourceCreated(eventSource);
                EnableEvents(
                    eventSource,
                    EventLevel.LogAlways,
                    EventKeywords.All
                );
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            string eventIdent;

            eventIdent = eventData.EventName;
            string text = $"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)} [{eventData.EventSource.Name}-{eventIdent}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";
            Console.WriteLine(text);

            lock (_consoleLock)
            {
                ConsoleColor origForeground = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(text);
                Console.ForegroundColor = origForeground;
            }
        }
    }
}
