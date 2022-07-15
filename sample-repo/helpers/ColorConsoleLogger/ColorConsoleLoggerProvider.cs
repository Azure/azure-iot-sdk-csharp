// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Devices.Logging
{
    /// <summary>
    /// The <see cref="ILoggerProvider"/> implementation that creates the <see cref="ColorConsoleLogger"/> instance.
    /// </summary>
    public class ColorConsoleLoggerProvider : ILoggerProvider
    {
        private readonly ColorConsoleLoggerConfiguration _config;
        private readonly ConcurrentDictionary<string, ColorConsoleLogger> _loggers = new ConcurrentDictionary<string, ColorConsoleLogger>();

        /// <summary>
        /// Initialize an instance of <see cref="ColorConsoleLoggerProvider"/> with the supplied <see cref="ColorConsoleLoggerConfiguration"/>.
        /// </summary>
        /// <param name="config">The <see cref="ColorConsoleLoggerConfiguration"/> settings to be used for logging.</param>
        public ColorConsoleLoggerProvider(ColorConsoleLoggerConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Create a new <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="categoryName">The category name for the <see cref="ILogger"/> instance. This is usually the class name where the logger is initialized.</param>
        /// <returns>The created <see cref="ILogger"/> instance.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new ColorConsoleLogger(_config));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
