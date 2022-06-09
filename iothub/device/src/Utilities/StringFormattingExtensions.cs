// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// String extension class for common operations.
    /// This class is used by the SDK and should not be directly used by applications.
    /// </summary>
    public static class StringFormattingExtensions
    {
        /// <summary>
        /// Format string to be displayed.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">The object to format.</param>
        /// <returns>A copy of format in which the format item or items have been replaced by the string representation of arg0.</returns>
        public static string FormatForUser(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        /// <summary>
        /// Format string to be displayed.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">The object to format.</param>
        /// <returns>A copy of format in which the format item or items have been replaced by the string representation of arg0.</returns>
        public static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        /// <summary>
        /// Format string to be displayed as an error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="activityId">The activity Id.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>A copy of format in which the format item or items have been replaced by the string representation of arg0.</returns>
        public static string FormatErrorForUser(this string message, string activityId, int errorCode)
        {
            return FormatForUser(message, activityId, DateTime.UtcNow, errorCode);
        }

        /// <summary>
        /// Truncates a string.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="maximumSize">The maximum string length.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string message, int maximumSize)
        {
            return (message?.Length ?? 0) > maximumSize
                ? message.Substring(0, maximumSize) + "...(truncated)"
                : message;
        }
    }
}
