// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace Microsoft.Azure.Devices.Logging
{
    /// <summary>
    /// The ILogger implementation for writing color log entries to console.
    /// For additional details, see https://docs.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-3.1.
    /// </summary>
    public class ColorConsoleLogger : ILogger
    {
        private static readonly object s_lockObject = new();

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

        public String logLevelColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    return "\x1b[0;31m";
                    //return "\u001b[31m";
                case LogLevel.Debug:
                    return "\x1b[0;33m";
                case LogLevel.Warning:
                    return "\x1b[4;33m";
                case LogLevel.Information:
                    return "\x1b[0;32m";
                case LogLevel.Critical:
                    return "\x1b[4;31m";
                case LogLevel.Trace:
                    return "\x1b[0;36m";

                default:
                    return "";
            }
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
        ///         Trace,        Debug,        Information,        Warning,        Error,        Critical,        None
   
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            String color = logLevelColor(logLevel);
            //ConsoleColor color = _config.LogLevelToColorMapping[logLevel];
            if (_config.EventIds.Contains(ColorConsoleLoggerConfiguration.DefaultEventId) || _config.EventIds.Contains(eventId.Id))
            {
                //ConsoleColor initialColor = Console.ForegroundColor;

                //this lock interfere with consoleEvent lock
                //lock (s_lockObject)
                //{
                //    Console.ForegroundColor = color;
                Console.WriteLine($"{color}{DateTime.Now:G} {logLevel} - {formatter(state, exception)}" + "\x1b[0m");
                //    Console.WriteLine($"{DateTime.Now:G} {logLevel} - {formatter(state, exception)}");
                //    Console.ForegroundColor = initialColor;
                //}
            }
        }
    }
}
