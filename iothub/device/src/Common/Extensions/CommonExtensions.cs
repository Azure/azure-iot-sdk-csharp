// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.Client.Extensions
{
    internal delegate bool TryParse<in TInput, TOutput>(TInput input, bool ignoreCase, out TOutput output);

    internal static class CommonExtensionMethods
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';

        // The following regex expression minifies a json string.
        // It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        // The first option @"(""(?:[^""\\]|\\.)*"")" matches a double quoted string.
        // The "(?:[^""\\])" indicates that the output (within quotes) is captured, and available as replacement in the Regex.Replace call below.
        // The "[^""\\]" matches any character except a double quote or escape character \.
        // The second option "\s+" matches all other space characters.
        private static readonly Regex s_trimWhiteSpace = new Regex(@"(""(?:[^""\\]|\\.)*"")|\s+", RegexOptions.Compiled);

        public static string EnsureStartsWith(this string value, char prefix)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Length == 0
                ? prefix.ToString(CultureInfo.InvariantCulture)
                : value[0] == prefix ? value : prefix + value;
        }

        public static string GetValueOrDefault(this IDictionary<string, string> map, string keyName)
        {
            if (!map.TryGetValue(keyName, out string value))
            {
                value = null;
            }

            return value;
        }

        public static IDictionary<string, string> ToDictionary(this string valuePairString, char kvpDelimiter, char kvpSeparator)
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed Token");
            }

            // This regex allows semi-colons to be part of the allowed characters for device names. Although new devices are not
            // allowed to have semi-colons in the name, some legacy devices still have them and so this name validation cannot be changed.
            IEnumerable<string[]> parts = new Regex($"(?:^|{kvpDelimiter})([^{kvpDelimiter}{kvpSeparator}]*){kvpSeparator}")
                .Matches(valuePairString)
                .Cast<Match>()
                .Select(m => new string[] {
                    m.Result("$1"),
                    valuePairString.Substring(
                        m.Index + m.Value.Length,
                        (m.NextMatch().Success ? m.NextMatch().Index : valuePairString.Length) - (m.Index + m.Value.Length))
                });

            if (!parts.Any() || parts.Any(p => p.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }

            IDictionary<string, string> map = parts.ToDictionary(kvp => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        public static bool TryGetIotHubName(this HttpRequestMessage requestMessage, out string iotHubName)
        {
            iotHubName = null;

            // header overrides the domain name, this is basically to help with testing so we don't have
            // to mess with the hosts file
            if (requestMessage.Headers.Contains(CustomHeaderConstants.HttpIotHubName))
            {
                iotHubName = requestMessage.Headers.GetValues(CustomHeaderConstants.HttpIotHubName).FirstOrDefault();
            }
            else
            {
                // {IotHubname}.[env-specific-sub-domain.]IotHub[-int].net
                //
                string[] hostNameParts = requestMessage.RequestUri.Host.Split('.');
                if (hostNameParts.Length < 3)
                {
                    return false;
                }
                iotHubName = hostNameParts[0];
            }

            return true;
        }

        public static string GetIotHubName(this HttpRequestMessage requestMessage)
        {
            return !TryGetIotHubName(requestMessage, out string iotHubName)
                ? throw new ArgumentException("Invalid request URI")
                : iotHubName;
        }

        public static void AppendKeyValuePairIfNotEmpty(this StringBuilder builder, string name, object value)
        {
            if (value != null)
            {
                builder.Append(name);
                builder.Append(ValuePairSeparator);
                builder.Append(value);
                builder.Append(ValuePairDelimiter);
            }
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static int NthIndexOf(this string str, char value, int startIndex, int n)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            if (startIndex < 0 || startIndex >= str.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n));
            }

            int entryIndex = -1;
            int nextSearchStartIndex = startIndex;
            for (int i = 0; i < n; i++)
            {
                entryIndex = str.IndexOf(value, nextSearchStartIndex);
                if (entryIndex < 0)
                {
                    return -1;
                }
                nextSearchStartIndex = entryIndex + 1;
            }

            return entryIndex;
        }

        /// <summary>
        /// Helper to remove extra white space from the supplied string.
        /// It makes sure that space characters within sentences are preserved, and all other space characters are discarded.
        /// </summary>
        /// <param name="input">The string to be formatted.</param>
        /// <returns>The input string, with extra white space removed. </returns>
        public static string TrimWhiteSpace(this string input)
        {
            return s_trimWhiteSpace.Replace(input, "$1");
        }
    }
}
