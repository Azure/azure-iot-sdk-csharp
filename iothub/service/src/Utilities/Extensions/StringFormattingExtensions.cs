// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// String extension class for common operations.
    /// This class is used by the SDK and should not be directly used by applications.
    /// </summary>
    internal static class StringFormattingExtensions
    {
        /// <summary>
        /// Format string to be displayed.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">The object to format.</param>
        /// <returns>A copy of format in which the format item or items have been replaced by the string representation of arg0.</returns>
        internal static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
