// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Logging
{
    /// <summary>
    /// Extension methods to help simplify creation of a new <see cref="ColorConsoleLoggerProvider"/> instance.
    /// </summary>
    public static class ColorConsoleLoggerExtensions
    {
        /// <summary>
        /// Add a new <see cref="ColorConsoleLoggerProvider"/> instance, with the supplied <see cref="ColorConsoleLoggerConfiguration"/> settings.
        /// </summary>
        /// <param name="loggerFactory">The type for which this extension method is defined.</param>
        /// <param name="config">The <see cref="ColorConsoleLoggerConfiguration"/> settings to be used for logging.</param>
        /// <returns>The <see cref="ILoggerFactory "/> instance.</returns>
        public static ILoggerFactory AddColorConsoleLogger(this ILoggerFactory loggerFactory, ColorConsoleLoggerConfiguration config)
        {
            loggerFactory.AddProvider(new ColorConsoleLoggerProvider(config));
            return loggerFactory;
        }

        /// <summary>
        /// Add a new <see cref="ColorConsoleLoggerProvider"/> instance, with the default <see cref="ColorConsoleLoggerConfiguration"/> settings.
        /// </summary>
        /// <param name="loggerFactory">The type for which this extension method is defined.</param>
        /// <returns>The <see cref="ILoggerFactory "/> instance.</returns>
        public static ILoggerFactory AddColorConsoleLogger(this ILoggerFactory loggerFactory)
        {
            var config = new ColorConsoleLoggerConfiguration();
            return loggerFactory.AddColorConsoleLogger(config);
        }
    }
}
