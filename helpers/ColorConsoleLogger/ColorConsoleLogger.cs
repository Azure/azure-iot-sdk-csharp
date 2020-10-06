// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Devices.Logging
{
    /// <summary>
    /// The ILogger implementation for writing color log entries to console.
    /// For additional details, see https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1.
    /// </summary>
    public class ColorConsoleLogger : ILogger
    {
        private readonly ColorConsoleLoggerConfiguration _config;

        /// <summary>
        /// Initializes an instance of <see cref="ColorConsoleLogger"/>.
        /// </summary>
        /// <param name="config">The <see cref="ColorConsoleLoggerConfiguration"/> settings to be used for logging.</param>
        public ColorConsoleLogger(ColorConsoleLoggerConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Begin a group of logical operations.
        /// </summary>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <param name="state">The identifier for the scope.</param>
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the given log level is enabled.
        /// </summary>
        /// <param name="logLevel">The log level to be checked.</param>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _config.MinLogLevel;
        }

        /// <summary>
        /// Writes the log entry to console output.
        /// </summary>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        /// <param name="logLevel">The log level of the log entry to be written.</param>
        /// <param name="eventId">The event Id of the log entry to be written.</param>
        /// <param name="state">The log entry to be written.</param>
        /// <param name="exception">The exception related to the log entry.</param>
        /// <param name="formatter">The formatter to be used for formatting the log message.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var color = _config.LogLevelToColorMapping[logLevel];
            if (_config.EventIds.Contains(ColorConsoleLoggerConfiguration.DefaultEventId) || _config.EventIds.Contains(eventId.Id))
            {
                ConsoleColor initialColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)}>> ");

                Console.ForegroundColor = color;
                Console.WriteLine($"{logLevel} - {formatter(state, exception)}");
                Console.ForegroundColor = initialColor;
            }
        }
    }
}
