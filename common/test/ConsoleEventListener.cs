// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Tracing
{
    public sealed class ConsoleEventListener : EventListener
    {
        private readonly string _eventFilter;
        private object _lock = new object();

        public ConsoleEventListener() : this(string.Empty) { }

        public ConsoleEventListener(string filter)
        {
            _eventFilter = filter ?? throw new ArgumentNullException(nameof(filter));

            foreach (EventSource source in EventSource.GetSources())
                EnableEvents(source, EventLevel.LogAlways);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (_lock)
            {
                string text = $"[{eventData.EventSource.Name}-{eventData.EventId}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";
                if (_eventFilter != null && text.Contains(_eventFilter))
                {
                    ConsoleColor origForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(text);
                    Debug.WriteLine(text);
                    Console.ForegroundColor = origForeground;
                }
            }
        }
    }
}
