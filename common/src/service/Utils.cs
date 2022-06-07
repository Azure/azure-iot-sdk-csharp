// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.Azure.Devices.Common
{
    internal static class Utils
    {
        public static bool IsValidBase64(string input, out int lengthInBytes)
        {
            lengthInBytes = 0;
            try
            {
                lengthInBytes = Convert.FromBase64String(input).Length;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            ValidateBufferBounds(buffer.Length, offset, size);
        }

        private static void ValidateBufferBounds(int bufferSize, int offset, int size)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, ApiResources.ArgumentMustBeNonNegative);
            }

            if (offset > bufferSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    offset,
                    ApiResources.OffsetExceedsBufferSize.FormatInvariant(bufferSize));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, ApiResources.ArgumentMustBePositive);
            }

            int remainingBufferSpace = bufferSize - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(size),
                    size,
                    ApiResources.SizeExceedsRemainingBufferSpace.FormatInvariant(remainingBufferSpace));
            }
        }

        public static string GetClientVersion()
        {
            var a = Assembly.GetExecutingAssembly();
            var attribute = (AssemblyInformationalVersionAttribute)a
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true)[0];
            return a.GetName().Name + "/" + attribute.InformationalVersion;
        }


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
    }
}
