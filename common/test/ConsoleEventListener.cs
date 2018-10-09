// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * To read colorized logs, analyze timings use https://marketplace.visualstudio.com/items?itemName=emilast.LogFileHighlighter 
 * 
 * Suggested color configuration :

   "logFileHighlighter.customPatterns": [
        {
            "pattern": ".*-Enter]",
            "foreground": "#42adf4"
        },
        {
            "pattern": ".*-Exit]",
            "foreground": "#cc99ff"
        },
        {
            "pattern": ".*-Associate]",
            "foreground": "magenta"
        },
        {
            "pattern": ".*-Info]",
            "foreground": "#11a046"
        },
        {
            "pattern": ".*-ErrorMessage]",
            "foreground": "red"
        },
        {
            "pattern": ".*-Critical]",
            "foreground": "#ea4112"
        },
        {
            "pattern": ".*-TestMessage]",
            "foreground": "gray"
        },
    ]
 *
 */

namespace System.Diagnostics.Tracing
{
    public sealed class ConsoleEventListener : EventListener
    {
        private readonly string[] _eventFilters;
        private object _lock = new object();

        public ConsoleEventListener() : this(string.Empty) { }

        public ConsoleEventListener(string filter)
        {
            _eventFilters = new string[1];
            _eventFilters[0] = filter ?? throw new ArgumentNullException(nameof(filter));

            InitializeEventSources();
        }

        public ConsoleEventListener(string [] filters)
        {
            _eventFilters = filters ?? throw new ArgumentNullException(nameof(filters));
            if (_eventFilters.Length == 0) throw new ArgumentException("Filters cannot be empty");

            foreach (string filter in _eventFilters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    throw new ArgumentNullException(nameof(filters));
                }
            }

            InitializeEventSources();
        }

        private void InitializeEventSources()
        {
            foreach (EventSource source in EventSource.GetSources())
            {
                EnableEvents(source, EventLevel.LogAlways);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
#if NET451
            EnableEvents(eventSource, EventLevel.LogAlways);
#else
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
#endif
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_eventFilters == null) return;

            lock (_lock)
            {
#if NET451
                string text = $"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")} [{eventData.EventSource.Name}-{eventData.EventId}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";
#else
                string text = $"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff")} [{eventData.EventSource.Name}-{eventData.EventName}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";
#endif
                bool shouldDisplay = false;

                if (_eventFilters.Length == 1 && text.Contains(_eventFilters[0]))
                {
                    shouldDisplay = true;
                }
                else
                {
                    foreach (string filter in _eventFilters)
                    {
                        if (text.Contains(filter))
                        {
                            shouldDisplay = true;
                        }
                    }
                }

                if (shouldDisplay)
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
