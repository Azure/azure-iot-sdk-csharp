// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.Devices.Common.WebApi;

namespace Microsoft.Azure.Devices.Common
{
    /// <summary>
    /// Tries to parse the input.
    /// </summary>
    /// <typeparam name="TInput">Input type parse.</typeparam>
    /// <typeparam name="TOutput">Parsed output type.</typeparam>
    /// <param name="input">Input object to parse.</param>
    /// <param name="ignoreCase">Specifies weather to ignore case or not.</param>
    /// <param name="output">Parsed output object.</param>
    /// <returns>True if the parsing was successful, otherwise returns false.</returns>
    public delegate bool TryParse<TInput, TOutput>(TInput input, bool ignoreCase, out TOutput output);

    /// <summary>
    /// Extension method helpers
    /// </summary>
    internal static class CommonExtensionMethods
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';

        /// <summary>
        /// Takes a string representation of key/value pairs and produces a dictionary
        /// </summary>
        /// <param name="valuePairString">The string containing key/value pairs</param>
        /// <param name="kvpDelimiter">The delimeter between key/value pairs</param>
        /// <param name="kvpSeparator">The character separating each key and value</param>
        /// <returns>A dictionary of the key/value pairs</returns>
        internal static IDictionary<string, string> ToDictionary(this string valuePairString, char kvpDelimiter, char kvpSeparator)
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed token");
            }

            IEnumerable<string[]> parts = valuePairString
                .Split(kvpDelimiter)
                .Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }

        /// <summary>
        /// Append a key value pair to a non-null <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The StringBuilder to append the key value pair to.</param>
        /// <param name="name">The key to be appended to the StringBuilder.</param>
        /// <param name="value">The value to be appended to the StringBuilder.</param>
        internal static void AppendKeyValuePairIfNotEmpty(this StringBuilder builder, string name, object value)
        {
            if (value != null)
            {
                builder.Append(name);
                builder.Append(ValuePairSeparator);
                builder.Append(value);
                builder.Append(ValuePairDelimiter);
            }
        }
    }
}
