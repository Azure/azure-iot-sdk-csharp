// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Samples
{
    /// <summary>
    /// A color console logger configuration that creates different color console entries per log level, sets the default log level to Information and logs all events.
    /// </summary>
    public class ColorConsoleLoggerConfiguration
    {
        // If the EventId is set to 0, the logger will log all events.
        internal const int DefaultEventId = 0;

        private static readonly IReadOnlyDictionary<LogLevel, ConsoleColor> s_defaultColorMapping = new Dictionary<LogLevel, ConsoleColor>
        {
            { LogLevel.Trace, ConsoleColor.Blue },
            { LogLevel.Debug, ConsoleColor.DarkYellow },
            { LogLevel.Information, ConsoleColor.Cyan },
            { LogLevel.Warning, ConsoleColor.DarkMagenta },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkRed }
        };

        /// <summary>
        /// Initialize an instance of <see cref="ColorConsoleLoggerConfiguration"/> with default color mappings.
        /// </summary>
        public ColorConsoleLoggerConfiguration()
        {
            LogLevelToColorMapping = s_defaultColorMapping;
        }

        /// <summary>
        /// Initialize an instance of <see cref="ColorConsoleLoggerConfiguration"/> by overriding the default color mappings with the supplied custom mappings.
        /// </summary>
        /// <param name="customConsoleColorMapping">A dictionary of log level to console color mapping that will be used to override the default color mapping.</param>
        public ColorConsoleLoggerConfiguration(IDictionary<LogLevel, ConsoleColor> customConsoleColorMapping)
            : this ()
        {
            var map = (IDictionary<LogLevel, ConsoleColor> )LogLevelToColorMapping;

            // If a custom color mapping is provided, use it to override the default color mapping.
            foreach (KeyValuePair<LogLevel, ConsoleColor> entry in customConsoleColorMapping)
            {
                if (map.ContainsKey(entry.Key))
                {
                    map[entry.Key] = entry.Value;
                }
            }
            LogLevelToColorMapping = (IReadOnlyDictionary<LogLevel, ConsoleColor>)map;
        }

        /// <summary>
        /// A dictionary containing the log level to console color mappings to be used while writing log entries to the console.
        /// </summary>
        public IReadOnlyDictionary<LogLevel, ConsoleColor> LogLevelToColorMapping { get; }

        /// <summary>
        /// The min log level that will be written to the console, defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The list of event Ids to be written to the console. By default, all event Ids are written.
        /// </summary>
        public IEnumerable<int> EventIds { get; set; } = new List<int>() { DefaultEventId };
    }
}
