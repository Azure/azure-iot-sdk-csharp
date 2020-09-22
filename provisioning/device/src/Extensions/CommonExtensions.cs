// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Provisioning.Client.Extensions
{
    internal static class CommonExtensions
    {
        /// <summary>
        /// Helper to remove extra whitespace from the supplied string.
        /// It makes sure that strings that contain space characters are preserved, and all other space characters are discarded.
        /// </summary>
        /// <param name="input">The string to be formatted.</param>
        /// <returns>The input string, with extra whitespace removed. </returns>
        public static string TrimWhiteSpace(this string input)
        {
            return Regex.Replace(input, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
        }

        /// <summary>
        /// Throw ArgumentNullException if the value is null reference, empty or white space.
        /// </summary>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="argumentName">The argument name.</param>
        internal static void ThrowIfNullOrWhiteSpace(this string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                string errorMessage = $"The parameter named {argumentName} can't be null, empty or white space.";
                throw new ArgumentNullException(argumentName, errorMessage);
            }
        }
    }
}
