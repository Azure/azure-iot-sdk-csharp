// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Common
{
    internal static class StringFormattingExtensions
    {
        public static string FormatForUser(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        public static string FormatErrorForUser(this string message, string activityId, int errorCode)
        {
            return Resources.UserErrorFormat.FormatForUser(message, activityId, DateTime.UtcNow, errorCode);
        }

        public static string Truncate(this string message, int maximumSize)
        {
            return message.Length > maximumSize ? message.Substring(0, maximumSize) + "...(truncated)" : message;
        }
    }
}
