// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Commong string extensions
    /// </summary>
    public static class CommonExtensions
    {
        // The following regex expression minifies a json string.
        // It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        // The first option @"(""(?:[^""\\]|\\.)*"")" matches a double quoted string.
        // The "(?:[^""\\])" indicates that the output (within quotes) is captured, and available as replacement in the Regex.Replace call below.
        // The "[^""\\]" matches any character except a double quote or escape character \.
        // The second option "\s+" matches all other space characters.
        private static readonly Regex s_trimWhiteSpace = new Regex(@"(""(?:[^""\\]|\\.)*"")|\s+", RegexOptions.Compiled);

        /// <summary>
        /// Helper to remove extra white space from the supplied string.
        /// It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        /// </summary>
        /// <param name="input">The string to be formatted.</param>
        /// <returns>The input string, with extra white space removed. </returns>
        internal static string TrimWhiteSpace(this string input)
        {
            return s_trimWhiteSpace.Replace(input, "$1");
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

        /// <summary>
        /// Get hostname
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetTarget(this Uri requestUri)
        {
            var requestUriLocalPath = requestUri.LocalPath;
            requestUriLocalPath = requestUriLocalPath.TrimStart('/');
            string[] parameters = requestUriLocalPath.Split('/');
            if (parameters.Length <= 3)
            {
                throw new ArgumentException($"Invalid RequestUri LocalPath");
            }

            return string.Concat(parameters[0], "/", parameters[1], "/", parameters[2]);
        }
    }
}